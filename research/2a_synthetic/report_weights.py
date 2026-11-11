#!/usr/bin/env python3
"""Turn the existing third-round simulation CSV into a reproducible review report.

Only synthetic research; no database or real construction inputs. No fabricated
results: all reported numbers are parsed from local experiment CSV.
"""
import argparse
import csv
import json
import math
from collections import defaultdict
from pathlib import Path

EXPECTED_CONFIGS = ("current_no_structure_gate", "current_with_structure_gate",
                    "no_site_area_with_gate")
NEEDED = ("scenario", "train_size", "config", "material",
          "mean_ape_percent", "mean_interval_coverage_percent",
          "mean_ess", "mean_cross_structure_percent", "skipped_cases")

def parse_summary(path):
    with path.open("r", encoding="utf-8-sig", newline="") as fh:
        reader = csv.DictReader(fh)
        if not set(NEEDED) <= set(reader.fieldnames or []):
            raise ValueError("Missing expected summary columns; rerun production_weight_review.py")
        records = []
        seen = set()
        for raw in reader:
            row = {k: raw[k] for k in NEEDED}
            if row["config"] not in EXPECTED_CONFIGS:
                raise ValueError("Unknown configuration: " + row["config"])
            row["train_size"] = int(row["train_size"])
            for key in ("mean_ape_percent", "mean_interval_coverage_percent",
                        "mean_ess", "mean_cross_structure_percent"):
                if row[key].strip() == "":
                    raise ValueError("Missing metric " + key + "; inspect insufficient reference cases")
                row[key] = float(row[key])
                if not math.isfinite(row[key]):
                    raise ValueError("Non-finite " + key)
            row["skipped_cases"] = int(row["skipped_cases"])
            if row["mean_ape_percent"] < 0 or not 0 <= row["mean_interval_coverage_percent"] <= 100:
                raise ValueError("Invalid MAPE or coverage")
            if row["mean_ess"] < 1 or row["mean_ess"] > row["train_size"] + .01:
                raise ValueError("ESS inconsistent with training size")
            if not 0 <= row["mean_cross_structure_percent"] <= 100:
                raise ValueError("Invalid cross-structure rate")
            key = (row["scenario"], row["train_size"], row["material"], row["config"])
            if key in seen:
                raise ValueError("Duplicate metric group: " + str(key))
            seen.add(key)
            records.append(row)
    if not records:
        raise ValueError("Summary CSV is empty")
    return records

def analyse(records):
    groups = defaultdict(dict)
    for row in records:
        key = (row["scenario"], row["train_size"], row["material"])
        groups[key][row["config"]] = row
    missing = [key for key, group in groups.items() if set(group) != set(EXPECTED_CONFIGS)]
    if missing:
        raise ValueError("Incomplete comparison sets; examples: " + str(missing[:3]))
    sections = []
    improvements = defaultdict(lambda: {"win": 0, "loss": 0, "tie": 0})
    for (scenario, size, material), group in sorted(groups.items()):
        baseline = group[EXPECTED_CONFIGS[0]]
        options = []
        for name in EXPECTED_CONFIGS:
            row = group[name]
            delta = round(row["mean_ape_percent"] - baseline["mean_ape_percent"], 3)
            options.append({
                "configuration": name, "mape_percent": row["mean_ape_percent"],
                "delta_mape_vs_current_pp": delta,
                "coverage_percent": row["mean_interval_coverage_percent"],
                "ess": row["mean_ess"],
                "cross_structure_percent": row["mean_cross_structure_percent"],
                "skipped_cases": row["skipped_cases"],
            })
            if name != EXPECTED_CONFIGS[0]:
                category = "win" if delta < -.001 else ("loss" if delta > .001 else "tie")
                improvements[name][category] += 1
        sections.append({"scenario":scenario,"train_size":size,"material":material,
                         "baseline_mape_percent":baseline["mean_ape_percent"],
                         "methods":options})
    return sections, dict(improvements)

def report(records, provenance):
    sections, outcomes = analyse(records)
    lines = [
        "# 2A 第三輪相似度權重：模擬結果檢視報告",
        "",
        "> **此報告僅由合成資料的本機 CSV 計算，不是崴正真實工程的準確率驗證。**",
        "> P15/P85 的區間涵蓋率僅是描述性，不是已校準的工程預測區間。",
        "",
        "## 來源與重現資訊",
        "",
        f"- 輸入摘要：`{provenance['summary_file']}`",
        f"- 實驗參數：`{provenance['manifest_file']}`",
        f"- 情境組數：{len(sections)}（情境 × 樣本數 × 材料）",
        "- 配置：現行無結構門檻、現行加結構門檻、移除基地面積權重加結構門檻。",
        "",
        "## 模擬誤差對照（MAPE %；Δ 為相較現行模型的百分點）",
        "",
        "| 情境 | 參考數 | 材料 | 現行 | 結構門檻 | Δ | 去基地面積 + 門檻 | Δ |",
        "|---|---:|---|---:|---:|---:|---:|---:|",
    ]
    for part in sections:
        data = {m["configuration"]:m for m in part["methods"]}
        a,b,c = (data[n] for n in EXPECTED_CONFIGS)
        lines.append(f"| {part['scenario']} | {part['train_size']} | {part['material']} | "
                     f"{a['mape_percent']:.2f} | {b['mape_percent']:.2f} | "
                     f"{b['delta_mape_vs_current_pp']:+.2f} | {c['mape_percent']:.2f} | "
                     f"{c['delta_mape_vs_current_pp']:+.2f} |")
    lines += ["", "## 勝負分布（以每個情境組的 MAPE 比較；數值較低視為改善）", ""]
    for config in EXPECTED_CONFIGS[1:]:
        count = outcomes[config]
        lines.append(f"- `{config}`：改善 {count['win']} 組、惡化 {count['loss']} 組、相同 {count['tie']} 組。")
    lines += ["", "## 跨結構混入與資料品質警訊", ""]
    all_methods = [m for part in sections for m in part["methods"]]
    for cfg in EXPECTED_CONFIGS:
        ms = [m for m in all_methods if m["configuration"] == cfg]
        lines.append(f"- `{cfg}`：跨結構平均比例 "
                     f"{sum(m['cross_structure_percent'] for m in ms)/len(ms):.2f}%；"
                     f"ESS 平均 {sum(m['ess'] for m in ms)/len(ms):.2f}；"
                     f"跳過案例加總 {sum(m['skipped_cases'] for m in ms)}。")
    lines += ["",
        "## 解讀與下一個實驗",
        "",
        "1. **不能僅以模擬勝負改生產權重。** 合成用量依建築用途與結構預設不同基準，結構門檻有可能天然較有利。",
        "2. **不要把模擬 MAPE 當作實際工程正確率。** 公司實際案與外部案的可比性、總量／部分工項範圍仍待確認。",
        "3. **下一輪應做公平條件與時間前推回測。** 同一目標涵蓋率比較區間，並排除待估工程日期之後的歷史資料。",
        "4. **產學正式算法不變。** 本結果只供專題研究與模型敏感度分析。",
        "",
    ]
    return "\n".join(lines)

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--summary", type=Path,
                   default=Path("research/2a_synthetic/output/production_weight_summary.csv"))
    p.add_argument("--manifest", type=Path,
                   default=Path("research/2a_synthetic/output/production_weight_manifest.json"))
    p.add_argument("--output", type=Path,
                   default=Path("research/2a_synthetic/output/production_weight_report.md"))
    args=p.parse_args()
    if not args.manifest.is_file():
        p.error("Manifest missing; generate dataset before analysis")
    meta=json.loads(args.manifest.read_text(encoding="utf-8-sig"))
    if meta.get("classification") != "SIMULATED_RESEARCH_ONLY" or meta.get("not_real_engineering_accuracy") is not True:
        p.error("Refusing manifest not labeled simulation-only")
    records=parse_summary(args.summary)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report(records, {"summary_file":args.summary.name,
                                            "manifest_file":args.manifest.name}),
                           encoding="utf-8")
    print(f"Simulation-only report written: {args.output} ({len(records)} comparison rows)")

if __name__=="__main__":
    main()
