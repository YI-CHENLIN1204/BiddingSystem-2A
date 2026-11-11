#!/usr/bin/env python3
"""Compare estimators on synthetic cases with strict project-level holdout.

Results are simulator diagnostics, not measured accuracy on real projects.
"""
import argparse
import csv
import json
import math
import statistics
from collections import defaultdict
from pathlib import Path

def quantile(sorted_values, p):
    if not sorted_values:
        raise ValueError("empty values")
    location = (len(sorted_values) - 1) * p
    lo, hi = math.floor(location), math.ceil(location)
    return sorted_values[lo] * (hi - location) + sorted_values[hi] * (location - lo)

def weighted_quantile(pairs, q):
    ordered = sorted((v, w) for v, w in pairs if w > 0)
    if not ordered:
        raise ValueError("no weights")
    goal = sum(w for _, w in ordered) * q
    running = 0.0
    for value, weight in ordered:
        running += weight
        if running >= goal:
            return value
    return ordered[-1][0]

def similarity_weight(target, sample, mode):
    # Research variant, not a validated structure-specific similarity model.
    ratio = abs(sample["floor_area_m2"] - target["floor_area_m2"]) / target["floor_area_m2"]
    area_score = max(0.0, 1.0 - ratio / 0.5)
    floor_diff = abs(sample["ground_floors"] - target["ground_floors"])
    floor_score = max(0.0, 1.0 - 0.03 * floor_diff)
    city_score = 1.0 if sample["city"] == target["city"] else 0.0
    if mode == "area_floors":
        score = (20 * area_score + 10 * floor_score + 5 * city_score) / 35
    elif mode == "area_only":
        score = area_score
    else:
        raise ValueError("bad mode")
    age = abs(sample["project_year"] - target["project_year"])
    return max(1e-9, score**2 * (0.5 ** (age / 3)))

def evaluate(group, train_size, method, weight_mode):
    if len(group) <= train_size:
        return None
    # Each evaluation target is excluded from training. Simulation seed drives
    # all generated data; fixed sorting ensures reruns are deterministic.
    errors, intervals, esizes = [], [], []
    for target_idx, target in enumerate(group):
        candidates = [v for i, v in enumerate(group) if i != target_idx]
        # Select prior observations deterministically; target never in train.
        training = candidates[:train_size]
        values = sorted(x["usage_per_m2"] for x in training)
        if method == "median":
            prediction = statistics.median(values)
            low, high = quantile(values, 0.15), quantile(values, 0.85)
        elif method == "normal":
            prediction = statistics.mean(values)
            sd = statistics.stdev(values) if len(values) > 1 else 0
            # Descriptive +/- 1 SD only; not a calibrated predictive interval.
            low, high = max(0, prediction-sd), prediction+sd
        elif method == "weighted":
            pairs = [(x["usage_per_m2"], similarity_weight(target, x, weight_mode))
                     for x in training]
            prediction = weighted_quantile(pairs, 0.5)
            low, high = weighted_quantile(pairs, 0.15), weighted_quantile(pairs, 0.85)
            weights = [w for _, w in pairs]
            esizes.append(sum(weights)**2 / sum(w*w for w in weights))
        else:
            raise ValueError("unsupported method")
        actual = target["usage_per_m2"]
        errors.append(abs(prediction - actual) / actual)
        intervals.append(low <= actual <= high)
    return {
        "evaluated_cases": len(group),
        "train_cases_per_target": train_size,
        "mean_absolute_percentage_error": round(sum(errors) / len(errors) * 100, 3),
        "interval_empirical_coverage_percent": round(sum(intervals)/len(intervals)*100, 2),
        "average_effective_training_cases": round(statistics.mean(esizes), 2) if esizes else None,
        "interval_warning": "Descriptive intervals are not calibrated predictive coverage",
    }

def main():
    p = argparse.ArgumentParser()
    p.add_argument("csv_file", type=Path)
    p.add_argument("--train-size", type=int, default=3)
    p.add_argument("--output", type=Path, default=None)
    args = p.parse_args()
    if args.train_size < 2:
        p.error("train-size must be >= 2")
    with args.csv_file.open(newline="", encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))
    if not rows or any(r.get("is_synthetic") != "true" or
                       r.get("dataset_type") != "synthetic_research" for r in rows):
        p.error("this evaluator only accepts the explicitly labeled synthetic dataset")
    groups = defaultdict(list)
    for row in rows:
        row["floor_area_m2"] = float(row["floor_area_m2"])
        row["usage_per_m2"] = float(row["usage_per_m2"])
        row["ground_floors"] = int(row["ground_floors"])
        row["project_year"] = int(row["project_year"])
        key = (row["project_type"], row["structure_type"], row["material"])
        groups[key].append(row)
    report = {
        "classification": "SYNTHETIC_SIMULATION_DIAGNOSTIC_NOT_REAL_ACCURACY",
        "train_size": args.train_size, "groups": [],
        "not_model_validation_on_real_construction": True,
    }
    for (kind, structure, material), cases in sorted(groups.items()):
        case_ids = [x["case_id"] for x in cases]
        if len(case_ids) != len(set(case_ids)):
            p.error(f"duplicate case ID in {kind}/{structure}/{material}")
        methods = {}
        for method in ("median", "normal", "weighted"):
            methods[method] = evaluate(cases, args.train_size, method, "area_floors")
        methods["weighted_area_only"] = evaluate(cases, args.train_size, "weighted", "area_only")
        report["groups"].append({
            "project_type": kind, "structure_type": structure, "material": material,
            "unique_projects": len(case_ids), "methods": methods,
        })
    output = args.output or args.csv_file.with_suffix(".comparison.json")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(f"Wrote {output} ({len(report['groups'])} groups); SIMULATION ONLY")

if __name__ == "__main__":
    main()
