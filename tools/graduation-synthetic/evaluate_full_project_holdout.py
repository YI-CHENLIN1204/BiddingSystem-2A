#!/usr/bin/env python3
"""Project-level, full-scope synthetic data holdout experiment.

Works on the v2 aligned BOQ/history dataset. Still NOT C# API E2E or real accuracy.
"""
import argparse
import csv
import hashlib
import json
import math
from collections import defaultdict
from datetime import date
from pathlib import Path
from evaluate_synthetic_holdout import similarity, predictions


def evaluate(source, top_k=8, min_sample=5):
    manifest = json.loads((source/"manifest.json").read_text(encoding="utf-8"))
    if manifest.get("generator_version") != "2.0.0" or manifest.get("data_origin") != "SYNTHETIC":
        raise ValueError("requires SYNTHETIC v2 dataset")
    def read(name):
        actual = hashlib.sha256((source/name).read_bytes()).hexdigest()
        if actual != manifest.get("files_sha256", {}).get(name):
            raise ValueError("SHA256 mismatch: " + name)
        with (source/name).open(encoding="utf-8-sig", newline="") as f:
            return list(csv.DictReader(f))
    projects = {p["synthetic_project_id"]:p for p in read("projects.csv")}
    txs = read("historical_transactions.csv")
    detail = read("project_boq_rows.csv")
    if len(txs) != manifest["transaction_count"] or len(detail) != manifest["boq_line_count"] or len(projects) != manifest["project_count"]:
        raise ValueError("manifest count mismatch")
    by_tx = {t["synthetic_transaction_id"]:t for t in txs}
    if len(by_tx) != len(txs) or len({r["synthetic_line_id"] for r in detail}) != len(detail):
        raise ValueError("duplicate line or transaction")
    for row in detail:
        tx = by_tx.get(row["synthetic_transaction_id"])
        if not tx or any(row[a] != tx[b] for a,b in [
            ("synthetic_project_id", "synthetic_project_id"), ("material_code", "material_code"),
            ("quantity","actual_quantity"), ("unit_price","unit_price"), ("split","split")]):
            raise ValueError("BOQ/history mismatch")
    # Never use separate line items of the same project as independent samples.
    aggregate = defaultdict(float)
    labels = defaultdict(set)
    for tx in txs:
        project = projects.get(tx["synthetic_project_id"])
        if project is None or tx["split"] != project["split"]:
            raise ValueError("orphan or split mismatch")
        key = (tx["material_code"], project["synthetic_project_id"])
        aggregate[key] += float(tx["actual_quantity"])
        labels[key].add(tx["synthetic_label"])
    train = defaultdict(list)
    test = []
    for (code,pid), qty in aggregate.items():
        project=projects[pid]
        usage=qty/float(project["total_floor_area"])
        clean=labels[(code,pid)] == {"baseline"}
        if project["split"] == "train" and clean:
            train[code].append((project,usage))
        elif project["split"] == "test" and clean:
            test.append((code,project,usage))
    outcomes = defaultdict(lambda: defaultdict(list))
    skipped = 0
    for code, target, actual in test:
        observations=[]
        for prior, usage in train[code]:
            score=similarity(target, prior)
            years=abs((date.fromisoformat(target["start_date"])-date.fromisoformat(prior["start_date"])).days)/365.2425
            weight=score*.5**(years/3)
            if weight>0:
                observations.append((score,weight,usage))
        observations.sort(key=lambda row:(-row[0],-row[1]))
        observations=observations[:top_k]
        if len(observations)<min_sample:
            skipped+=1
            continue
        empirical, normal=predictions([(u,w) for _,w,u in observations])
        for method, (lo,mid,hi) in (
            ("weighted_quantile_proxy",empirical),
            ("weighted_normal_proxy",normal)
        ):
            outcomes[method][code].append((actual,lo,mid,hi))
    def metric(rows):
        n=len(rows)
        if not n:
            return {"n":0,"status":"INSUFFICIENT_DATA"}
        alpha=.30
        def score(v):
            actual,lo,_,hi=v
            return (hi-lo)+(2/alpha)*(lo-actual if actual<lo else actual-hi if actual>hi else 0)
        return {"n":n,
                "mape_percent":round(sum(abs(v[2]-v[0])/v[0] for v in rows)/n*100,3),
                "p15_p85_coverage_percent":round(sum(v[1]<=v[0]<=v[3] for v in rows)/n*100,2),
                "mean_interval_score":round(sum(score(v) for v in rows)/n,5)}
    report={}
    for method in ("weighted_quantile_proxy","weighted_normal_proxy"):
        per=outcomes[method]
        flat=[v for rows in per.values() for v in rows]
        # Interval score has units: only calculate by item code, never
        # average physical unit-bearing scores across incomparable materials.
        report[method]={"n":len(flat),
                        "overall_mape_percent":metric(flat).get("mape_percent"),
                        "overall_coverage_percent":metric(flat).get("p15_p85_coverage_percent"),
                        "per_material":{code:metric(rows) for code,rows in sorted(per.items())}}
    return {
      "origin":"SYNTHETIC","benchmark":"OFFLINE_FULL_BOQ_PROXY_NOT_CSHARP_API",
      "project_count":len(projects),
      "boq_line_count":len(detail),
      "historical_line_count":len(txs),
      "distinct_material_codes":len({t["material_code"] for t in txs}),
      "eligible_test_project_material_pairs":len(test),
      "excluded_insufficient_reference_pairs":skipped,
      "top_k":top_k,"min_samples":min_sample,
      "method_comparison":report,
      "disclaimer":"All values synthetic; not real construction accuracy. Per-project-material aggregate avoids treating repeated work lines as independent observations; C# service not run."
    }


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--output",type=Path,required=True)
    parser.add_argument("--top-k",type=int,default=8)
    parser.add_argument("--min-sample",type=int,default=5)
    args=parser.parse_args()
    if args.output.exists(): parser.error("refusing overwrite")
    if args.top_k<1 or args.min_sample<1:parser.error("invalid sample parameters")
    try:
        result=evaluate(args.source,args.top_k,args.min_sample)
    except (ValueError,KeyError,OSError,json.JSONDecodeError) as exc:
        parser.error(str(exc))
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({k:v for k,v in result.items() if k!="method_comparison"},ensure_ascii=False,indent=2))
    print(json.dumps({k:{x:y for x,y in v.items() if x!="per_material"} for k,v in result["method_comparison"].items()},ensure_ascii=False,indent=2))


if __name__=="__main__":
    main()
