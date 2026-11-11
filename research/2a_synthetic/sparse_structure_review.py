#!/usr/bin/env python3
"""Stress-test scarce same-structure histories: 0, 1, 2, or 3 cases.

Simulation only. A target project is never a reference. This experiment does
NOT validate the production model or add synthetic cases to official history.
"""
import argparse
import csv
import json
import statistics
from collections import defaultdict
from pathlib import Path

from generate import produce
from production_weight_review import predict

CONFIGS = ("current_no_structure_gate", "current_with_structure_gate")
SPARSE_COUNTS = (0, 1, 2, 3)
MINIMUM_OFFICIAL_REFERENCE_COUNT = 5

def prepare(seed, scenario, reservoir_per_combo=20):
    rows = produce(seed, reservoir_per_combo + 1, scenario)
    groups = defaultdict(list)
    for r in rows:
        item = dict(r)
        for field in ("floor_area_m2", "site_area_m2", "usage_per_m2"):
            item[field] = float(item[field])
        for field in ("ground_floors", "basement_floors", "project_year"):
            item[field] = int(item[field])
        groups[item["project_type"], item["structure_type"], item["material"]].append(item)
    return groups

def stress(seeds, sparse_counts=SPARSE_COUNTS, scenarios=("stable", "high_variance", "company_shift", "outliers"),
           reservoir_per_combo=20, minimum_count=MINIMUM_OFFICIAL_REFERENCE_COUNT):
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError("nonempty unique seeds required")
    if any(n < 0 or n >= reservoir_per_combo for n in sparse_counts):
        raise ValueError("invalid sparse count")
    result = []
    for scenario in scenarios:
        for seed in seeds:
            groups = prepare(seed, scenario, reservoir_per_combo)
            for (kind, structure, material), cases in sorted(groups.items()):
                # Last synthetic case is always target; earlier cases are references.
                target = cases[-1]
                others = [row for (k, s, m), sample in groups.items()
                          if k == kind and m == material and s != structure
                          for row in sample[:-1]]
                for n in sparse_counts:
                    same = cases[:n]
                    candidates = same + others
                    for config in CONFIGS:
                        # The production-style comparator normally requires two
                        # selections to compute weighted quantiles; return null
                        # with <2 eligible candidates.
                        prediction = predict(target, candidates, 8, config)
                        # Count eligibility without artificially filling the target
                        # same-structure sample pool.
                        compatible = n if config == "current_with_structure_gate" else n + len(others)
                        eligible_enough = compatible >= minimum_count
                        actual_selected = prediction["selected"] if prediction else min(compatible, 8)
                        # We report technical computability separately from
                        # the configured minimum-reference-count readiness.
                        ready = prediction is not None and actual_selected >= minimum_count
                        result.append({
                            "scenario": scenario, "seed": seed,
                            "project_type": kind, "structure_type": structure, "material": material,
                            "same_structure_histories": n, "alternative_structure_histories": len(others),
                            "config": config, "minimum_sample_count": minimum_count,
                            "eligible_reference_count": compatible,
                            "selected_reference_count": actual_selected,
                            "reference_count_gate_pass": bool(ready),
                            "technical_prediction_available": prediction is not None,
                            "simulated_ape_percent": round(abs(prediction["median"]-target["usage_per_m2"])
                                                            / target["usage_per_m2"]*100, 4)
                                                        if prediction is not None else "",
                            "cross_structure_reference_percent": round(
                                prediction["cross_structure_fraction"]*100, 2
                            ) if prediction is not None else "",
                            "effective_sample_size": round(prediction["ess"], 3) if prediction else "",
                        })
    return result

def summarize(rows):
    groups = defaultdict(list)
    for row in rows:
        groups[row["scenario"], row["same_structure_histories"], row["config"]].append(row)
    summaries = []
    for (scenario, n, config), cases in sorted(groups.items()):
        ready=[row for row in cases if row["reference_count_gate_pass"]]
        technical=[row for row in cases if row["technical_prediction_available"]]
        summaries.append({
            "scenario":scenario, "same_structure_histories":n, "config":config,
            "target_cases":len(cases),
            "ready_count":len(ready),
            "ready_rate_percent":round(100*len(ready)/len(cases),2),
            "technical_predictions":len(technical),
            "mean_technical_ape_percent":round(statistics.mean(
                r["simulated_ape_percent"] for r in technical),3) if technical else "",
            "mean_cross_structure_percent":round(statistics.mean(
                r["cross_structure_reference_percent"] for r in technical),2) if technical else "",
            "mean_ess":round(statistics.mean(r["effective_sample_size"]
                                               for r in technical),2) if technical else "",
        })
    return summaries

def write_csv(path, rows):
    if not rows:
        raise ValueError("empty rows")
    path.parent.mkdir(parents=True,exist_ok=True)
    with path.open("w",newline="",encoding="utf-8-sig") as f:
        writer=csv.DictWriter(f,fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

def report(summary, output):
    lines=[
        "# 2A 第六輪：同結構稀少歷史參考壓力測試（模擬）", "",
        "> 0～3 筆同結構歷史，另外提供其他結構的虛構候選案。",
        "> 『技術上能輸出』與『滿足最低 5 筆正式參考門檻』分開計算；此處的 5 是沿用既有設定，不代表足以證明工程可靠度。",
        "> MAPE 只描述人為模擬結果，不是崴正真實建案準確率。", "",
        "| 情境 | 同結構案 | 模型 | 準備率 | 技術可算個數 | 模擬 MAPE % | 混用其他結構 % |",
        "|---|---:|---|---:|---:|---:|---:|",
    ]
    for row in summary:
        lines.append(f"| {row['scenario']} | {row['same_structure_histories']} | "
                     f"{row['config']} | {row['ready_rate_percent']:.1f}% | "
                     f"{row['technical_predictions']} | "
                     f"{row['mean_technical_ape_percent'] if row['mean_technical_ape_percent'] != '' else '—'} | "
                     f"{row['mean_cross_structure_percent'] if row['mean_cross_structure_percent'] != '' else '—'} |")
    lines += ["", "## 限制", "",
        "- 實驗設計每個類型取最後一筆作目標，對同類型其餘案例限制僅 0～3 筆；其他結構仍各有 20 筆虛構候選，不符合所有公司的真實分布。",
        "- 模型未採時間前推，而且假設不同結構有預設的材料基準差異；高跨結構誤差可能由產生器的假設主導。",
        "- 同結構硬門檻少於 5 筆時應返回資料不足，不應以模擬資料冒充正式參考；無門檻即使通過名義筆數，仍須審查來源可比性。",
        "- 『技術上可計算』不是產學可以直接出異常燈號的充分條件；正式 .NET 程式不在此輪變動。",
        ""]
    output.write_text("\n".join(lines),encoding="utf-8")

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--first-seed",type=int,default=20261111)
    p.add_argument("--seeds",type=int,default=5)
    p.add_argument("--reservoir-per-combo",type=int,default=20)
    p.add_argument("--output",type=Path,default=Path("research/2a_synthetic/output/sparse_structure"))
    args=p.parse_args()
    if args.seeds<1 or args.seeds>100 or args.reservoir_per_combo<4:
        p.error("require seeds 1..100; reservoir-per-combo >= 4")
    seeds=list(range(args.first_seed,args.first_seed+args.seeds))
    rows=stress(seeds,reservoir_per_combo=args.reservoir_per_combo)
    summary=summarize(rows)
    prefix=args.output
    write_csv(prefix.with_name(prefix.name+"_detail.csv"),rows)
    write_csv(prefix.with_name(prefix.name+"_summary.csv"),summary)
    report(summary,prefix.with_name(prefix.name+"_report.md"))
    prefix.with_name(prefix.name+"_manifest.json").write_text(json.dumps({
        "classification":"SIMULATED_RESEARCH_ONLY",
        "not_real_engineering_accuracy":True,
        "minimum_sample_count":MINIMUM_OFFICIAL_REFERENCE_COUNT,
        "sparse_counts":list(SPARSE_COUNTS),
        "seeds":seeds,
        "reservoir_per_combo":args.reservoir_per_combo,
        "scenarios":["stable","high_variance","company_shift","outliers"],
        "detail_rows":len(rows),"summary_rows":len(summary),
        "production_model_not_modified":True,
        "note":"Technical computability != policy readiness; cross-structure references are synthetic.",
    },ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(f"Sparse synthetic-only stress: {len(rows)} target-configuration runs, "
          f"{len(summary)} summaries; official DB unchanged")

if __name__=="__main__":
    main()
