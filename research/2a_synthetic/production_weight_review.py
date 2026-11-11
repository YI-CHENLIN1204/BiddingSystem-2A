#!/usr/bin/env python3
"""Audit the six-factor 2A similarity and alternative structure gates.

EXPERIMENT ONLY. The scoring terms mirror ProjectSimilarityService and
DetectionCategoryRangeService at the 2026-11-11 audit snapshot; no DB access.
Cases are wholly synthetic; never interpret MAPE as real engineering accuracy.
"""
import argparse
import csv
import json
import math
import statistics
from collections import defaultdict
from pathlib import Path

from generate import produce
from compare import weighted_quantile

DEFAULT_WEIGHTS = {"structure": 25., "area": 20., "site": 10.,
                   "floors": 10., "year": 0., "city": 5.}
ALTERNATIVE_WEIGHTS = {"structure": 25., "area": 25., "site": 0.,
                       "floors": 15., "year": 0., "city": 5.}
CONFIGS = {
    "current_no_structure_gate": (DEFAULT_WEIGHTS, False),
    "current_with_structure_gate": (DEFAULT_WEIGHTS, True),
    "no_site_area_with_gate": (ALTERNATIVE_WEIGHTS, True),
}
FIELDS = ("scenario", "seed", "project_type", "structure_type", "material",
          "train_size", "config", "evaluated_cases", "skipped_cases",
          "mean_ape_percent", "interval_coverage_percent",
          "mean_effective_sample_size", "cross_structure_references_percent")

def score(target, sample, weights=DEFAULT_WEIGHTS, area_cutoff=.5, site_cutoff=.5,
          floor_ground_penalty=.03, floor_basement_penalty=.10, year_cutoff=6):
    """Reproduce six independent production factor scores / sum(weights)*100."""
    if target["project_type"] != sample["project_type"]:
        return 0.
    total = sum(weights.values())
    if total <= 0:
        raise ValueError("nonpositive weight total")
    def area_ratio(key, cutoff):
        a, b = target[key], sample[key]
        if a <= 0 or b <= 0:
            return 0.
        return max(0., 1. - abs(b-a)/a/cutoff)
    structure = float(target["structure_type"] == sample["structure_type"])
    floors = max(0., 1. - abs(target["ground_floors"]-sample["ground_floors"])
                 * floor_ground_penalty - abs(target["basement_floors"]-sample["basement_floors"])
                 * floor_basement_penalty)
    year = max(0., 1.-abs(target["project_year"]-sample["project_year"])/year_cutoff)
    raw = (weights["structure"]*structure +
           weights["area"]*area_ratio("floor_area_m2", area_cutoff) +
           weights["site"]*area_ratio("site_area_m2", site_cutoff) +
           weights["floors"]*floors +
           weights["year"]*year +
           weights["city"]*float(target["city"] == sample["city"]))
    # C# project similarity rounds the returned score to 2 decimals.
    return round(raw/total*100., 2)

def predict(target, references, train_size, config, topk=8,
            exponent=2., half_life_years=3.):
    weights, gate = CONFIGS[config]
    eligible = []
    for sample in references:
        if sample["case_id"] == target["case_id"]:
            continue
        if sample["project_type"] != target["project_type"]:
            continue
        if gate and sample["structure_type"] != target["structure_type"]:
            continue
        similarity = score(target, sample, weights)
        if similarity <= 0:
            continue
        # Same order as the 2A service: similarity first, then final weight.
        # Year difference is symmetric, matching current production behaviour.
        time_weight = .5 ** (abs(target["project_year"]-sample["project_year"]) /
                             half_life_years)
        final_weight = (similarity / 100.) ** exponent * time_weight
        eligible.append((sample, similarity, final_weight))
    eligible.sort(key=lambda item: (-item[1], -item[2], item[0]["case_id"]))
    selected = eligible[:min(train_size, topk)]
    if len(selected) < 2:
        return None
    pairs = [(s["usage_per_m2"], w) for s, _, w in selected]
    weights = [w for _, w in pairs]
    sw = sum(weights)
    ess = sw*sw/sum(w*w for w in weights)
    cross = sum(1 for s,_,_ in selected
                if s["structure_type"] != target["structure_type"]) / len(selected)
    return {
        "median": weighted_quantile(pairs, .5),
        "low": weighted_quantile(pairs, .15),
        "high": weighted_quantile(pairs, .85),
        "ess": ess,
        "cross_structure_fraction": cross,
        "selected": len(selected),
    }

def run(seeds, cases_per_combo=30, train_sizes=(3,5,8),
        scenarios=("stable","high_variance","company_shift","outliers")):
    results = []
    for scenario in scenarios:
        for seed in seeds:
            rows = []
            for row in produce(seed, cases_per_combo, scenario):
                item = dict(row)
                for key in ("floor_area_m2", "site_area_m2", "usage_per_m2"):
                    item[key] = float(item[key])
                for key in ("ground_floors", "basement_floors", "project_year"):
                    item[key] = int(item[key])
                rows.append(item)
            groups = defaultdict(list)
            for item in rows:
                groups[(item["project_type"],item["structure_type"],item["material"])].append(item)
            for (kind,structure,material), targets in sorted(groups.items()):
                # Can compare to same material and use, while optionally mixing structures.
                pool = [x for x in rows if x["material"] == material
                        and x["project_type"] == kind]
                for size in train_sizes:
                    for config in CONFIGS:
                        ape, covered, ess, cross = [], [], [], []
                        skipped = 0
                        for target in targets:
                            prediction = predict(target, pool, size, config)
                            if prediction is None:
                                skipped += 1
                                continue
                            truth = target["usage_per_m2"]
                            ape.append(abs(prediction["median"]-truth)/truth*100.)
                            covered.append(prediction["low"]<=truth<=prediction["high"])
                            ess.append(prediction["ess"])
                            cross.append(prediction["cross_structure_fraction"]*100.)
                        results.append({
                            "scenario":scenario, "seed":seed, "project_type":kind,
                            "structure_type":structure, "material":material,
                            "train_size":size, "config":config,
                            "evaluated_cases":len(ape), "skipped_cases":skipped,
                            "mean_ape_percent":round(statistics.mean(ape),3) if ape else "",
                            "interval_coverage_percent":round(statistics.mean(covered)*100.,2) if covered else "",
                            "mean_effective_sample_size":round(statistics.mean(ess),2) if ess else "",
                            "cross_structure_references_percent":round(statistics.mean(cross),2) if cross else "",
                        })
    return results

def summarize(rows):
    by_group=defaultdict(list)
    for r in rows:
        by_group[(r["scenario"],r["train_size"],r["config"],r["material"])].append(r)
    result=[]
    for (scenario,size,config,material), members in sorted(by_group.items()):
        valid = [r for r in members if r["evaluated_cases"]]
        result.append({
            "scenario":scenario, "train_size":size, "config":config,
            "material":material, "group_runs":len(members),
            "mean_ape_percent":round(statistics.mean(r["mean_ape_percent"] for r in valid),3) if valid else "",
            "mean_interval_coverage_percent":round(statistics.mean(r["interval_coverage_percent"] for r in valid),2) if valid else "",
            "mean_ess":round(statistics.mean(r["mean_effective_sample_size"] for r in valid),2) if valid else "",
            "mean_cross_structure_percent":round(statistics.mean(r["cross_structure_references_percent"] for r in valid),2) if valid else "",
            "skipped_cases":sum(r["skipped_cases"] for r in members),
        })
    return result

def write_csv(path, rows):
    with path.open("w", newline="",encoding="utf-8-sig") as f:
        writer=csv.DictWriter(f, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--first-seed",type=int,default=20261111)
    parser.add_argument("--seeds",type=int,default=5)
    parser.add_argument("--cases-per-combo",type=int,default=30)
    parser.add_argument("--train-sizes",type=int,nargs="+",default=[3,5,8])
    parser.add_argument("--output",type=Path,default=Path("research/2a_synthetic/output/production_weight"))
    args=parser.parse_args()
    if args.seeds<1 or args.seeds>100 or min(args.train_sizes)<2 or max(args.train_sizes)>8:
        parser.error("require seeds 1..100, train-sizes 2..8")
    if args.cases_per_combo <= max(args.train_sizes):
        parser.error("cases-per-combo must exceed the largest train size")
    seeds=list(range(args.first_seed,args.first_seed+args.seeds))
    records=run(seeds,args.cases_per_combo,tuple(args.train_sizes))
    aggregates=summarize(records)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    write_csv(args.output.with_name(args.output.name+"_detail.csv"),records)
    write_csv(args.output.with_name(args.output.name+"_summary.csv"),aggregates)
    metadata={
        "classification":"SIMULATED_RESEARCH_ONLY",
        "not_real_engineering_accuracy":True,
        "model_scope":"six factor production-inspired score and time weights; no database access",
        "production_model_not_modified":True,
        "config_weights":{k:dict(weights=v[0],structure_gate=v[1])
                          for k,v in CONFIGS.items()},
        "seeds":seeds,"scenarios":["stable","high_variance","company_shift","outliers"],
        "train_sizes":args.train_sizes,"cases_per_combo":args.cases_per_combo,
        "detail_rows":len(records),"summary_rows":len(aggregates),
        "limitation":"Synthetic unit-consumption baselines are arbitrary; no price-major-category validation; includes future-dated references as in current scoring",
    }
    args.output.with_name(args.output.name+"_manifest.json").write_text(
        json.dumps(metadata,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(f"Weight audit: {len(records)} group runs / {len(aggregates)} summary rows; SYNTHETIC ONLY")

if __name__=="__main__":
    main()
