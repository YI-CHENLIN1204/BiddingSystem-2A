#!/usr/bin/env python3
"""Paired factorial design to disentangle structure gate and weight changes.

Synthetic research only. Uses the same source cases for each configuration.
The original production_weight_review.py CLI and three configurations stay intact.
"""
import argparse
import csv
import json
import statistics
from collections import defaultdict
from pathlib import Path

from production_weight_review import (
    DEFAULT_WEIGHTS, ALTERNATIVE_WEIGHTS, run, summarize
)

NO_SITE_ONLY = {**DEFAULT_WEIGHTS, "site": 0.0}
FACTORIAL_CONFIGS = {
    "A_baseline_no_gate": (DEFAULT_WEIGHTS, False),
    "B_baseline_gate": (DEFAULT_WEIGHTS, True),
    "C_redistributed_gate": (ALTERNATIVE_WEIGHTS, True),
    "D_redistributed_no_gate": (ALTERNATIVE_WEIGHTS, False),
    "E_remove_site_only_gate": (NO_SITE_ONLY, True),
    "F_remove_site_only_no_gate": (NO_SITE_ONLY, False),
}
# These are pairwise comparisons holding all other design dimensions fixed.
PAIRS = {
    "gate_effect_baseline_weights": ("A_baseline_no_gate", "B_baseline_gate"),
    "gate_effect_redistributed_weights": ("D_redistributed_no_gate", "C_redistributed_gate"),
    "gate_effect_no_site_weights": ("F_remove_site_only_no_gate", "E_remove_site_only_gate"),
    "remove_site_only_without_gate": ("A_baseline_no_gate", "F_remove_site_only_no_gate"),
    "remove_site_only_with_gate": ("B_baseline_gate", "E_remove_site_only_gate"),
    "redistribute_without_gate": ("A_baseline_no_gate", "D_redistributed_no_gate"),
    "redistribute_with_gate": ("B_baseline_gate", "C_redistributed_gate"),
    "redistribute_vs_remove_site_with_gate": ("E_remove_site_only_gate", "C_redistributed_gate"),
}

def comparisons(summary):
    index = defaultdict(dict)
    for row in summary:
        key = row["scenario"], row["train_size"], row["material"]
        if row["config"] in index[key]:
            raise ValueError("duplicate factorial summary group")
        index[key][row["config"]] = row
    output = []
    for key, group in sorted(index.items()):
        if set(group) != set(FACTORIAL_CONFIGS):
            raise ValueError("incomplete factorial summary group")
        for label, (before, after) in PAIRS.items():
            a, b = group[before], group[after]
            if not isinstance(a["mean_ape_percent"], (int,float)) or not isinstance(b["mean_ape_percent"], (int,float)):
                raise ValueError("missing MAPE; inspect skipped cases")
            output.append({
                "scenario":key[0], "train_size":key[1], "material":key[2],
                "comparison":label, "before":before, "after":after,
                "before_mape_percent":a["mean_ape_percent"],
                "after_mape_percent":b["mean_ape_percent"],
                "delta_mape_pp":round(b["mean_ape_percent"]-a["mean_ape_percent"], 3),
                "before_cross_structure_percent":a["mean_cross_structure_percent"],
                "after_cross_structure_percent":b["mean_cross_structure_percent"],
                "before_ess":a["mean_ess"], "after_ess":b["mean_ess"],
                "before_skipped":a["skipped_cases"], "after_skipped":b["skipped_cases"],
            })
    return output

def save(path, rows):
    if not rows:
        raise ValueError("no data")
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8-sig") as file:
        writer = csv.DictWriter(file, fieldnames=rows[0].keys())
        writer.writeheader()
        writer.writerows(rows)

def write_markdown(rows, path):
    labels = sorted({r["comparison"] for r in rows})
    lines = [
        "# 2A 第四輪：權重與結構門檻因子拆解（模擬研究）",
        "",
        "> 所有結果來自自行生成的假設工程數量，不是崴正真實工程準確率。",
        "> ΔMAPE < 0 代表在本模擬情境中誤差下降；不代表該設定可直接上線。",
        "",
        "| 單因子配對比較 | 情境組數 | 平均 ΔMAPE（百分點） | 改善 | 惡化 | 相同 |",
        "|---|---:|---:|---:|---:|---:|",
    ]
    for name in labels:
        subset = [row["delta_mape_pp"] for row in rows if row["comparison"] == name]
        win=sum(d < -.001 for d in subset)
        loss=sum(d > .001 for d in subset)
        same=len(subset)-win-loss
        lines.append(f"| {name} | {len(subset)} | {statistics.mean(subset):+.3f} | {win} | {loss} | {same} |")
    lines += [
        "", "## 實驗限制", "",
        "1. 此實驗將『是否硬限制結構』與『是否使用基地面積、是否將基地面積權重重新分配』獨立交叉。",
        "2. 每個目標案在所有方案下使用同一份候選建案集合；但不同評分可能選中不同 TopK。",
        "3. 同結構樣本每組仍有多件，故尚不能代表企業實際每組僅 1～3 件時的情境。",
        "4. 現行時間距離可使用比目標案年份更晚的參考；這不是真正時間前推回測。",
        "5. 此處比較的是材料單位面積用量，不是完整結構體工程標單價格。",
        "6. 目前沒有企業真實案例 holdout 或統計顯著性驗證，禁止直接修改正式權重。",
        "",
    ]
    path.write_text("\n".join(lines), encoding="utf-8")

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--first-seed", type=int, default=20261111)
    p.add_argument("--seeds", type=int, default=5)
    p.add_argument("--cases-per-combo", type=int, default=30)
    p.add_argument("--train-sizes", type=int, nargs="+", default=[3,5,8])
    p.add_argument("--output", type=Path,
                   default=Path("research/2a_synthetic/output/factorial_weight"))
    args = p.parse_args()
    if not 1 <= args.seeds <= 100 or not args.train_sizes:
        p.error("seeds 1..100 and nonempty train sizes required")
    if min(args.train_sizes) < 2 or max(args.train_sizes) > 8 or args.cases_per_combo <= max(args.train_sizes):
        p.error("require 2 <= train sizes <= 8 and cases_per_combo > max train size")
    seeds = list(range(args.first_seed,args.first_seed+args.seeds))
    details = run(seeds, args.cases_per_combo, tuple(args.train_sizes),
                  configurations=FACTORIAL_CONFIGS)
    summary = summarize(details)
    paired = comparisons(summary)
    root=args.output
    save(root.with_name(root.name+"_detail.csv"),details)
    save(root.with_name(root.name+"_summary.csv"),summary)
    save(root.with_name(root.name+"_paired.csv"),paired)
    write_markdown(paired,root.with_name(root.name+"_report.md"))
    root.with_name(root.name+"_manifest.json").write_text(json.dumps({
        "classification":"SIMULATED_RESEARCH_ONLY",
        "not_real_engineering_accuracy":True,
        "not_time_forward":True,
        "seeds":seeds,"cases_per_combo":args.cases_per_combo,
        "train_sizes":args.train_sizes,"configs":{
            k: {"weights":v[0], "structure_gate":v[1]}
            for k,v in FACTORIAL_CONFIGS.items()
        },"pairwise_comparisons":PAIRS,
        "detail_rows":len(details),"summary_rows":len(summary),
        "paired_rows":len(paired),
    },ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(f"Factorial synthetic-only benchmark: {len(details)} group runs; "
          f"{len(summary)} summaries; {len(paired)} pairwise rows")

if __name__=="__main__":
    main()
