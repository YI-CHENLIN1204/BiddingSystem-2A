#!/usr/bin/env python3
"""Multi-seed, multi-scenario synthetic research benchmark.

This is an exchangeability simulation, NOT an engineering calibration nor
a chronological out-of-sample validation. No databases are accessed.
"""
import argparse
import csv
import io
import json
import math
import statistics
from collections import defaultdict
from pathlib import Path

from compare import evaluate
from generate import produce

SCENARIOS = ("stable", "high_variance", "company_shift", "outliers")
TRAIN_SIZES = (3, 5, 8)
METHODS = ("median", "normal", "weighted", "weighted_area_only")

def prepare(seed, cases_per_combo, scenario):
    groups = defaultdict(list)
    for row in produce(seed, cases_per_combo, scenario):
        r = dict(row)
        for field in ("floor_area_m2", "usage_per_m2"):
            r[field] = float(r[field])
        for field in ("ground_floors", "project_year"):
            r[field] = int(r[field])
        key = (r["project_type"], r["structure_type"], r["material"])
        groups[key].append(r)
    return groups

def benchmark(seeds, cases_per_combo=30, train_sizes=TRAIN_SIZES,
              scenarios=SCENARIOS):
    if not seeds or len(seeds) != len(set(seeds)):
        raise ValueError("seeds must be nonempty and unique")
    if cases_per_combo <= max(train_sizes):
        raise ValueError("cases_per_combo must be > largest training size")
    results = []
    for scenario in scenarios:
        for seed in seeds:
            groups = prepare(seed, cases_per_combo, scenario)
            for (kind, structure, material), cases in sorted(groups.items()):
                for n in train_sizes:
                    for method in METHODS:
                        method_arg = "weighted" if method == "weighted_area_only" else method
                        mode = "area_only" if method == "weighted_area_only" else "area_floors"
                        metrics = evaluate(cases, n, method_arg, mode)
                        if metrics is None:
                            raise ValueError("unexpected insufficient simulation cases")
                        results.append({
                            "scenario": scenario, "seed": seed,
                            "project_type": kind, "structure_type": structure,
                            "material": material, "train_size": n,
                            "method": method,
                            "mape_percent": metrics["mean_absolute_percentage_error"],
                            "empirical_coverage_percent": metrics["interval_empirical_coverage_percent"],
                            "effective_train_cases": metrics["average_effective_training_cases"],
                            "evaluated_cases": metrics["evaluated_cases"],
                        })
    return results

def summarize(records):
    grouped = defaultdict(list)
    for item in records:
        key = (item["scenario"], item["train_size"], item["method"], item["material"])
        grouped[key].append(item)
    summary = []
    for (scenario, n, method, material), values in sorted(grouped.items()):
        # Average across seeds & project/structure groups, not pooled raw
        # observations, so a large group cannot silently dominate.
        mape = [v["mape_percent"] for v in values]
        coverage = [v["empirical_coverage_percent"] for v in values]
        ess = [v["effective_train_cases"] for v in values
               if v["effective_train_cases"] is not None]
        summary.append({
            "scenario": scenario, "train_size": n, "method": method,
            "material": material, "scenario_group_runs": len(values),
            "mean_mape_percent": round(statistics.mean(mape), 3),
            "sd_mape_percent": round(statistics.stdev(mape), 3) if len(mape) > 1 else 0.0,
            "mean_empirical_coverage_percent": round(statistics.mean(coverage), 2),
            "mean_effective_train_cases": round(statistics.mean(ess), 2) if ess else None,
        })
    return summary

def save_csv(path, rows):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not rows:
        raise ValueError("no rows")
    with path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--first-seed", type=int, default=20261111)
    parser.add_argument("--seeds", type=int, default=10)
    parser.add_argument("--cases-per-combo", type=int, default=30)
    parser.add_argument("--train-sizes", type=int, nargs="+", default=list(TRAIN_SIZES))
    parser.add_argument("--output", type=Path,
                        default=Path("research/2a_synthetic/output/batch"))
    args = parser.parse_args()
    if args.seeds < 2 or args.seeds > 200:
        parser.error("--seeds must be 2..200")
    if args.cases_per_combo <= max(args.train_sizes) or min(args.train_sizes) < 2:
        parser.error("require 2 <= train sizes < cases-per-combo")
    seeds = [args.first_seed + i for i in range(args.seeds)]
    records = benchmark(seeds, args.cases_per_combo, tuple(args.train_sizes))
    summary = summarize(records)
    save_csv(args.output.with_name(args.output.name + "_detail.csv"), records)
    save_csv(args.output.with_name(args.output.name + "_summary.csv"), summary)
    manifest = {
        "classification": "SYNTHETIC_RESEARCH_ONLY",
        "does_not_measure_real_world_accuracy": True,
        "comparison_kind": "project_level_holdout_random_reference_exchangeability",
        "not_time_forward": True,
        "note": "Normal mean +/- sample SD is descriptive, not calibrated predictive interval",
        "scenarios": list(SCENARIOS),
        "seeds": seeds, "cases_per_combo": args.cases_per_combo,
        "train_sizes": list(args.train_sizes), "methods": list(METHODS),
        "detail_rows": len(records), "summary_rows": len(summary),
    }
    dest = args.output.with_name(args.output.name + "_manifest.json")
    dest.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
                    encoding="utf-8")
    print(f"Simulation-only benchmark completed: {len(records)} group runs, "
          f"{len(summary)} summary rows; files under {args.output.parent}")

if __name__ == "__main__":
    main()
