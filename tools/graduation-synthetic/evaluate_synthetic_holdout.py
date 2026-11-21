#!/usr/bin/env python3
"""Offline, SYNTHETIC-ONLY holdout comparison. NOT live ASP.NET accuracy."""
import argparse
import csv
import hashlib
import json
import math
from collections import defaultdict
from datetime import date
from pathlib import Path
from statistics import NormalDist


def load(source):
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("data_origin") != "SYNTHETIC":
        raise ValueError("This benchmark only accepts SYNTHETIC input")
    for name in ("projects.csv", "historical_transactions.csv"):
        actual = hashlib.sha256((source / name).read_bytes()).hexdigest()
        if manifest.get("files_sha256", {}).get(name) != actual:
            raise ValueError("SHA256 mismatch: " + name)
    def rows(name):
        with (source / name).open(newline="", encoding="utf-8-sig") as stream:
            return list(csv.DictReader(stream))
    projects = rows("projects.csv")
    tx = rows("historical_transactions.csv")
    if len(projects) != manifest["project_count"] or len(tx) != manifest["transaction_count"]:
        raise ValueError("source count mismatch")
    return projects, tx


def similarity(a, b):
    # EXPLICITLY an offline proxy, not the application's ProjectSimilarityService.
    if a["project_type"] != b["project_type"]:
        return 0.0
    structure = 0.32 if a["structure_type"] == b["structure_type"] else 0.0
    area_a, area_b = float(a["total_floor_area"]), float(b["total_floor_area"])
    area = 0.40 * math.exp(-abs(math.log(area_a / area_b)))
    floors = 0.18 * math.exp(-abs(int(a["floor_count"]) - int(b["floor_count"])) / 7)
    city = 0.10 if a["city"] == b["city"] else 0.0
    return structure + area + floors + city


def quantile(observations, q):
    ordered = sorted(observations, key=lambda v: v[0])
    total = sum(w for _, w in ordered)
    if not ordered or total <= 0:
        raise ValueError("no weighted samples")
    cumulative = 0.0
    for val, weight in ordered:
        cumulative += weight
        if cumulative >= total * q:
            return val
    return ordered[-1][0]


def predictions(observations):
    empirical = [quantile(observations, q) for q in (0.15, 0.50, 0.85)]
    total = sum(w for _, w in observations)
    mean = sum(x * w for x, w in observations) / total
    variance = sum(w * (x - mean)**2 for x, w in observations) / total
    sd = math.sqrt(max(0.0, variance))
    normal = [max(0.0, NormalDist(mean, sd).inv_cdf(q)) if sd > 0 else mean
              for q in (0.15, 0.50, 0.85)]
    return empirical, normal


def evaluate(source, top_k=8, min_sample=5, half_life_years=3.0):
    if top_k < 1 or min_sample < 1 or half_life_years <= 0:
        raise ValueError("invalid parameters")
    projects, transactions = load(source)
    by_id = {p["synthetic_project_id"]: p for p in projects}
    train = defaultdict(list)
    holdout = []
    anomaly_test = 0
    for tx in transactions:
        project = by_id.get(tx["synthetic_project_id"])
        if not project or project["split"] != tx["split"] or tx["data_origin"] != "SYNTHETIC":
            raise ValueError("invalid transaction project/split")
        if float(tx["actual_quantity"]) <= 0 or float(project["total_floor_area"]) <= 0:
            continue
        key = tx["material_code"]
        if project["split"] == "train" and tx["synthetic_label"] == "baseline":
            train[key].append((project, float(tx["actual_quantity"]) / float(project["total_floor_area"])))
        elif project["split"] == "test":
            if tx["synthetic_label"] == "injected_anomaly":
                anomaly_test += 1
            else:
                holdout.append((key, project, float(tx["actual_quantity"]) / float(project["total_floor_area"])))
    records = []
    for material, target, actual_unit in holdout:
        refs = []
        for prior, unit in train[material]:
            sim = similarity(target, prior)
            years = abs((date.fromisoformat(target["start_date"]) -
                         date.fromisoformat(prior["start_date"])).days) / 365.2425
            weight = (sim ** 1.0) * (0.5 ** (years / half_life_years))
            if weight > 0:
                refs.append((sim, weight, unit))
        # Sort by similarity, consistent with service top-K candidate selection.
        refs.sort(key=lambda v: (-v[0], -v[1]))
        refs = refs[:top_k]
        if len(refs) < min_sample:
            continue
        emp, normal = predictions([(x, w) for _, w, x in refs])
        for method, (lo, mid, hi) in (("weighted_quantile_proxy", emp), ("weighted_normal_proxy", normal)):
            records.append({
                "project_id": target["synthetic_project_id"], "material_code": material,
                "method": method, "actual_usage_per_area": actual_unit,
                "lower": lo, "median": mid, "upper": hi, "reference_count": len(refs)})
    summary = {}
    for method in ("weighted_quantile_proxy", "weighted_normal_proxy"):
        subset = [x for x in records if x["method"] == method]
        n = len(subset)
        if n == 0:
            summary[method] = {"n": 0, "status": "INSUFFICIENT_DATA"}
            continue
        # Proper 70% central prediction interval score (lower is better).
        alpha = 0.30
        interval_score = lambda r: (r["upper"] - r["lower"]) + (
            (2 / alpha) * (r["lower"] - r["actual_usage_per_area"]) if r["actual_usage_per_area"] < r["lower"] else 0
        ) + (
            (2 / alpha) * (r["actual_usage_per_area"] - r["upper"]) if r["actual_usage_per_area"] > r["upper"] else 0
        )
        summary[method] = {
            "n": n,
            "median_absolute_error_unit_per_m2": round(sum(abs(x["median"]-x["actual_usage_per_area"]) for x in subset)/n, 6),
            "median_absolute_percentage_error_percent": round(sum(abs(x["median"]-x["actual_usage_per_area"]) / x["actual_usage_per_area"] for x in subset)/n*100, 3),
            "p15_p85_coverage_percent": round(sum(x["lower"] <= x["actual_usage_per_area"] <= x["upper"] for x in subset)/n*100, 2),
            "avg_interval_width_unit_per_m2": round(sum(x["upper"]-x["lower"] for x in subset)/n, 6),
            "interval_score_unit_per_m2": round(sum(interval_score(x) for x in subset)/n, 6)
        }
    return {
        "data_origin": "SYNTHETIC", "benchmark_kind": "OFFLINE_PROXY_NOT_API",
        "statement": "Only synthetic holdout sanity check. NOT engineering accuracy, not a real bid benchmark.",
        "training_split": "train", "testing_split": "test",
        "anomalous_training_samples_excluded": True,
        "anomalous_test_samples_excluded": anomaly_test,
        "holdout_baseline_records": len(holdout),
        "top_k": top_k, "min_sample": min_sample,
        "model_limitation": "Simplified similarity score, not direct C# service invocation; one project may contribute multiple material tests.",
        "metrics_by_method": summary
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--top-k", type=int, default=8)
    parser.add_argument("--min-sample", type=int, default=5)
    args = parser.parse_args()
    if args.output.exists():
        parser.error("refusing to overwrite existing evaluation report")
    try:
        result = evaluate(args.source, args.top_k, args.min_sample)
    except (ValueError, OSError, KeyError, json.JSONDecodeError) as exc:
        parser.error(str(exc))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
