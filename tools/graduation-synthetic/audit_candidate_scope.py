#!/usr/bin/env python3
"""Research-only gate for candidate materials; NEVER grants production red/green.

Offline proxy metrics are not formal C# API validation or real-engineering proof.
"""
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

from evaluate_full_project_holdout import evaluate


CORE = {"SYN-REBAR", "SYN-CONCRETE"}


def audit(source, min_test_projects=20):
    with (source / "projects.csv").open(encoding="utf-8-sig", newline="") as f:
        projects = {p["synthetic_project_id"]: p for p in csv.DictReader(f)}
    with (source / "project_boq_rows.csv").open(encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    names = {}
    counts = defaultdict(set)
    for row in rows:
        code = row["material_code"]
        names.setdefault(code, row["item_name"])
        if code not in CORE:
            counts[(code, row["split"], projects[row["synthetic_project_id"]]["structure_type"])].add(
                row["synthetic_project_id"])
    candidates = [code for code, name in names.items() if code == "SYN-STEEL"
                  or ("模板" in name or "土方" in name or "回填" in name)]
    benchmark = evaluate(source)
    per = benchmark["method_comparison"]["weighted_quantile_proxy"]["per_material"]
    result = []
    for code in sorted(candidates):
        metric = per.get(code, {"n": 0})
        train_by_structure = {s: len(counts[(code, "train", s)]) for s in ("RC", "SC", "SRC")}
        test_by_structure = {s: len(counts[(code, "test", s)]) for s in ("RC", "SC", "SRC")}
        sample_gate = (metric["n"] >= min_test_projects
                       and all(test_by_structure[s] >= 5 for s in ("RC", "SC", "SRC")
                               if train_by_structure[s] > 0))
        accuracy_gate = (metric.get("mape_percent") is not None
                         and metric.get("p15_p85_coverage_percent") is not None
                         and metric["mape_percent"] <= 25
                         and 60 <= metric["p15_p85_coverage_percent"] <= 90)
        result.append({
            "material_code": code,
            "example_name": names[code],
            "train_projects_by_structure": train_by_structure,
            "test_projects_by_structure": test_by_structure,
            "offline_proxy": metric,
            "preliminary_sample_gate": sample_gate,
            "preliminary_proxy_gate": accuracy_gate,
            "formal_red_green": False,
            "status": "BLOCKED_REQUIRES_LIVE_API_BACKTEST_AND_ENGINEERING_APPROVAL",
        })
    return {
        "data_origin": "SYNTHETIC",
        "benchmark": "OFFLINE_PROXY_NOT_REAL_CSHARP_API",
        "official_scope_unchanged": sorted(CORE),
        "candidate_count": len(result),
        "candidates": result,
        "policy": ("No candidate enters formal traffic-light output until independent "
                   "C# API holdout backtest, adequate per-structure samples, reviewed "
                   "engineering assumptions, and explicit scope-policy approval."),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error("refusing overwrite")
    report = audit(args.source)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n",
                           encoding="utf-8")
    print(json.dumps({"candidate_count": report["candidate_count"],
                      "formal_traffic_light_enabled": 0,
                      "output": str(args.output)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
