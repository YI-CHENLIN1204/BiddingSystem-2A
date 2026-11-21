#!/usr/bin/env python3
"""Read-only 2A range-API benchmark of existing SYNTHETIC v2 data.

No case uploads, analysis runs, history writes or settings updates. Parameters
are selected using validation projects only; test projects are scored afterwards.
Counterfactuals replay the inspected six-factor C# rules on API reference pools.
They must reproduce the live baseline before they are used for comparison.
"""
import argparse
import csv
import hashlib
import json
import math
import re
from collections import Counter, defaultdict
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import date, datetime, timezone
from decimal import Decimal, ROUND_HALF_EVEN
from pathlib import Path
from urllib.parse import urlsplit
from urllib.request import Request, urlopen

SCOPES = {"rebar": ("SYN-REBAR", "/api/detection-category/1/range"),
          "concrete": ("SYN-CONCRETE", "/api/detection-category/2/range"),
          "price": (None, "/api/major-category-price/range")}
FACTORS = ("structure", "area", "siteArea", "floor", "year", "city")
PING = 3.305785


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=True).encode()).hexdigest()


def read_csv(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def load_source(source):
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("data_origin") != "SYNTHETIC" or manifest.get("generator_version") != "2.0.0":
        raise ValueError("Only existing SYNTHETIC v2 sources are accepted")
    names = ("projects.csv", "historical_transactions.csv", "project_boq_rows.csv")
    for name in names:
        if hashlib.sha256((source / name).read_bytes()).hexdigest() != manifest["files_sha256"][name]:
            raise ValueError("Source SHA mismatch: " + name)
    projects = read_csv(source / names[0])
    by_id = {p["synthetic_project_id"]: p for p in projects}
    history = read_csv(source / names[1])
    detail = read_csv(source / names[2])
    if (len(projects), len(history), len(detail)) != (manifest["project_count"], manifest["transaction_count"], manifest["boq_line_count"]):
        raise ValueError("Manifest counts do not match")
    if len(by_id) != len(projects):
        raise ValueError("Duplicate project IDs")
    txs = {t["synthetic_transaction_id"]: t for t in history}
    if len(txs) != len(history):
        raise ValueError("Duplicate transaction IDs")
    # Aggregate within projects. Repeated BOQ rows never become independent cases.
    totals = defaultdict(lambda: {"actual": Decimal(0), "clean_actual": Decimal(0), "shocked_lines": 0, "lines": 0})
    line_ids = set()
    for row in detail:
        t = txs.get(row["synthetic_transaction_id"])
        p = by_id.get(row["synthetic_project_id"])
        if not t or not p or row["synthetic_line_id"] in line_ids:
            raise ValueError("Orphan or duplicate BOQ row")
        line_ids.add(row["synthetic_line_id"])
        if any(row[a] != t[b] for a, b in (("synthetic_project_id", "synthetic_project_id"), ("material_code", "material_code"), ("quantity", "actual_quantity"), ("unit_price", "unit_price"), ("split", "split"))):
            raise ValueError("BOQ/history alignment failure")
        if row["split"] != p["split"] or p["data_origin"] != "SYNTHETIC" or t["data_origin"] != "SYNTHETIC":
            raise ValueError("Split or source mismatch")
        if t["synthetic_label"] not in ("baseline", "injected_anomaly"):
            raise ValueError("Unknown source label")
        shocked = t["synthetic_label"] == "injected_anomaly"
        scopes = [s for s, (code, _) in SCOPES.items() if code == row["material_code"]]
        if row["major_category"] == "結構體工程":
            scopes.append("price")
        for scope in scopes:
            value = Decimal(row["amount"] if scope == "price" else row["quantity"])
            agg = totals[(p["synthetic_project_id"], scope)]
            agg["actual"] += value
            if not shocked:
                agg["clean_actual"] += value
            agg["shocked_lines"] += int(shocked)
            agg["lines"] += 1
    return manifest, by_id, totals


def target_dto(p):
    return {"projectType": p["project_type"], "structureType": p["structure_type"], "city": p["city"],
            "totalFloorArea": float(p["total_floor_area"]), "siteArea": float(p["site_area"]),
            "floorCount": int(p["floor_count"]), "basementFloorCount": int(p["basement_floor_count"]),
            "projectDate": p["start_date"]}


def api_json(api, method, path, body=None):
    # Whitelist by operation, not just HTTP verb. These POST routes only estimate.
    allowed_get = {"/api/settings", "/api/analysis-cases"}
    if not ((method == "GET" and path in allowed_get) or
            (method == "POST" and path in {v[1] for v in SCOPES.values()})):
        raise ValueError("Refusing a non-whitelisted operation")
    data = None if body is None else json.dumps(body, ensure_ascii=False).encode("utf-8")
    req = Request(api + path, data=data, method=method,
                  headers={"Content-Type": "application/json; charset=utf-8"})
    with urlopen(req, timeout=90) as response:
        return json.load(response)


def similarity(target, prior, settings, weights):
    def ratio(key, cutoff):
        a, b = Decimal(target[key]), Decimal(prior[key])
        return max(Decimal(0), 1 - abs(b-a)/a/Decimal(str(settings[cutoff])))
    parts = [Decimal(int(target["structure_type"] == prior["structure_type"])),
             ratio("total_floor_area", "areaDifferenceCutoffRatio"),
             ratio("site_area", "siteAreaDifferenceCutoffRatio"),
             max(Decimal(0), 1 - abs(int(target["floor_count"])-int(prior["floor_count"]))*Decimal(str(settings["groundFloorPenaltyRatio"])) - abs(int(target["basement_floor_count"])-int(prior["basement_floor_count"]))*Decimal(str(settings["basementFloorPenaltyRatio"]))),
             max(Decimal(0), 1 - Decimal(abs(date.fromisoformat(target["start_date"]).year-date.fromisoformat(prior["start_date"]).year))/Decimal(settings["yearDifferenceCutoffYears"])),
             Decimal(int(target["city"].casefold() == prior["city"].casefold()))]
    raw = sum(part*Decimal(str(weight)) for part, weight in zip(parts, weights))
    return float((raw/sum(Decimal(str(w)) for w in weights)*100).quantize(Decimal(".01"), rounding=ROUND_HALF_EVEN))


def weighted_quantiles(rows, quantiles):
    if not rows or any(not math.isfinite(x) or not math.isfinite(w) or w <= 0 for x, w in rows):
        raise ValueError("Invalid reference values/weights")
    ordered = sorted(rows)
    total = sum(w for _, w in ordered)
    result = []
    for q in quantiles:
        cumulative = 0.0
        for value, weight in ordered:
            cumulative += weight
            if cumulative >= total*q:
                result.append(value)
                break
        else:
            result.append(ordered[-1][0])
    return result


def validate_reference(ref, target, scope, projects, totals):
    match = re.match(r"^SYNTHETIC:(SYN-\d{5})(?:\s|$)", ref.get("projectName", ""))
    if not match or match[1] not in projects:
        raise ValueError("Reference has no recognized synthetic lineage")
    pid, p = match[1], projects[match[1]]
    if p["split"] != "train" or pid == target["synthetic_project_id"]:
        raise ValueError("Holdout leakage into API references")
    if p["project_type"] != target["project_type"] or (target["structure_type"] == "SRC" and p["structure_type"] != "SRC"):
        raise ValueError("Project type/SRC cohort violation")
    area = float(ref["floorAreaM2"] if scope == "price" else ref["floorArea"])
    if abs(area-float(p["total_floor_area"])) > .005 or ref["projectDate"][:10] != p["start_date"]:
        raise ValueError("Reference metadata differs from source")
    actual = float(ref["totalAmount"] if scope == "price" else ref["actualQuantity"])
    agg = totals[(pid, scope)]
    # Historical quantities exclude IsOutlier rows; retain the observed contract.
    if min(abs(actual-float(agg[k])) for k in ("actual", "clean_actual")) > .02:
        raise ValueError("Reference numeric total differs from source: " + pid + "/" + scope)
    if actual <= 0 or not math.isfinite(actual):
        raise ValueError("Nonpositive reference")
    return pid


def baseline_config(settings):
    return {"id": "current", "weights": [settings[f+"Weight"] for f in FACTORS],
            "top_k": settings["topK"], "half_life": settings["timeHalfLifeYears"],
            "exponent": settings["similarityExponent"]}


def predict(record, config, settings, projects):
    target = projects[record["project_id"]]
    refs = []
    for order, ref in enumerate(record["pool"]["references"]):
        prior = projects[ref["source_project_id"]]
        score = similarity(target, prior, settings, config["weights"])
        if score < settings["minimumSimilarityScore"] or score <= 0:
            continue
        years = abs((date.fromisoformat(target["start_date"])-date.fromisoformat(prior["start_date"])).days)/365.2425
        time_weight = .5**(years/config["half_life"]) if config["half_life"] is not None else 1.0
        weight = (score/100)**config["exponent"]*time_weight
        if record["scope"] == "price":
            value = float(ref["totalAmount"])/(float(ref["floorAreaM2"])/PING)
        else:
            value = float(ref["actualQuantity"])/float(ref["floorArea"])
        refs.append((score, weight, order, value, ref["projectId"]))
    refs.sort(key=lambda x: (-x[0], -x[1], x[2]))
    refs = refs[:config["top_k"]]
    if len(refs) < settings["minimumSampleCount"]:
        return None
    q = weighted_quantiles([(r[3], r[1]) for r in refs], [settings[k] for k in ("lowerQuantile", "medianQuantile", "upperQuantile")])
    factor = float(target["total_floor_area"])/(PING if record["scope"] == "price" else 1)
    return {"bounds": [v*factor for v in q], "reference_ids": [r[4] for r in refs],
            "ess": sum(r[1] for r in refs)**2/sum(r[1]**2 for r in refs)}


def api_bounds(response, scope):
    keys = ("lowerAmount", "medianAmount", "upperAmount") if scope == "price" else ("lowerQuantity", "medianQuantity", "upperQuantity")
    return [float(response[k]) for k in keys] if response.get("hasEnoughSamples") else None


def collect_one(project, scope, api, cache, signature, projects, totals, settings):
    pid = project["synthetic_project_id"]
    path = cache/(pid+"-"+scope+".json")
    if path.exists():
        record = json.loads(path.read_text(encoding="utf-8"))
        if record.get("signature") != signature:
            raise ValueError("Cached source/settings mismatch")
    else:
        extra = {"majorCategory": "結構體工程"} if scope == "price" else {}
        common = {"target": target_dto(project), **extra}
        baseline = api_json(api, "POST", SCOPES[scope][1], {**common, "topCount": settings["topK"]})
        pool = api_json(api, "POST", SCOPES[scope][1], {**common, "topCount": len(projects)})
        record = {"signature": signature, "project_id": pid, "scope": scope, "baseline": baseline, "pool": pool}
    pool = record["pool"]
    if len(pool["references"]) != pool["referenceProjectCount"] or pool["referenceProjectCount"] != pool["availableProjectCount"]:
        raise ValueError("Incomplete API reference pool")
    seen = set()
    for ref in pool["references"]:
        source_id = validate_reference(ref, project, scope, projects, totals)
        if ref["projectId"] in seen:
            raise ValueError("Duplicate API reference project")
        seen.add(ref["projectId"])
        ref["source_project_id"] = source_id
        expected_score = similarity(project, projects[source_id], settings, baseline_config(settings)["weights"])
        if abs(expected_score-float(ref["similarityScore"])) > .011:
            raise ValueError("Six-factor score replay differs from live service")
    replay = predict(record, baseline_config(settings), settings, projects)
    live = api_bounds(record["baseline"], scope)
    if (replay is None) != (live is None):
        raise ValueError("Replay/live sample sufficiency mismatch")
    if replay:
        if replay["reference_ids"] != [r["projectId"] for r in record["baseline"]["references"]]:
            raise ValueError("Replay/live TopK selection mismatch")
        if any(abs(a-b) > .025 for a, b in zip(replay["bounds"], live)):
            raise ValueError("Replay/live interval mismatch: " + pid + "/" + scope)
    path.write_text(json.dumps(record, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    return record


def metrics(rows):
    usable = [r for r in rows if r["bounds"] is not None]
    if not usable:
        return {"n": 0, "insufficient": len(rows)}
    def interval_loss(r):
        lo, _, hi = r["bounds"]
        y = r["actual"]
        return (hi-lo + 2/.3*max(lo-y, 0) + 2/.3*max(y-hi, 0))/y
    n = len(usable)
    return {"n": n, "insufficient": len(rows)-n,
            "coverage_percent": round(sum(r["bounds"][0] <= r["actual"] <= r["bounds"][2] for r in usable)/n*100, 3),
            "mae": round(sum(abs(r["bounds"][1]-r["actual"]) for r in usable)/n, 4),
            "mape_percent": round(sum(abs(r["bounds"][1]-r["actual"])/r["actual"] for r in usable)/n*100, 3),
            "relative_interval_score": sum(interval_loss(r) for r in usable)/n}


def evaluate(records, config, settings, projects, totals, live=False):
    groups = defaultdict(list)
    details = []
    for record in records:
        p = projects[record["project_id"]]
        agg = totals[(record["project_id"], record["scope"])]
        estimate = predict(record, config, settings, projects)
        bounds = api_bounds(record["baseline"], record["scope"]) if live else (estimate["bounds"] if estimate else None)
        row = {"project_id": record["project_id"], "scope": record["scope"], "structure": p["structure_type"],
               "actual": float(agg["actual"]), "bounds": bounds,
               "shocked_lines": agg["shocked_lines"], "extreme_scenario": p.get("extreme_scenario") or None}
        details.append(row)
        groups[record["scope"]+"/"+p["structure_type"]].append(row)
    all_groups = {key: metrics(rows) for key, rows in sorted(groups.items())}
    clean_groups = {key: metrics([r for r in rows if not r["shocked_lines"] and not r["extreme_scenario"]]) for key, rows in sorted(groups.items())}
    # Dimensionless macro score: never average kg, m3 and NTD absolute errors.
    scores = [g["relative_interval_score"] for g in clean_groups.values() if g["n"]]
    return {"config": config, "all_source_observations": all_groups,
            "unshocked_nonextreme_observations": clean_groups,
            "selection_score": sum(scores)/len(scores) if scores else None,
            "details": details}


def select_validation(candidates):
    eligible = [c for c in candidates if c["selection_score"] is not None and
                not any(m["insufficient"] for m in c["all_source_observations"].values())]
    if not eligible:
        raise ValueError("No fully covered validation candidate")
    return min(eligible, key=lambda c: (c["selection_score"], c["config"]["id"]))["config"]


def configs(settings):
    current = baseline_config(settings)
    yield current
    weight_sets = {"current": current["weights"], "geometry": [20, 40, 15, 15, 5, 5], "structure": [40, 25, 10, 15, 5, 5]}
    for label, weights in weight_sets.items():
        for k in (5, 8, 16, 32):
            for half in (1, 3, 6, None):
                for exponent in (0, 1, 2):
                    cfg = {"id": f"{label}-k{k}-h{half}-e{exponent}", "weights": weights, "top_k": k, "half_life": half, "exponent": exponent}
                    if any(cfg[key] != current[key] for key in ("weights", "top_k", "half_life", "exponent")):
                        yield cfg


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--api", default="http://127.0.0.1:5263")
    parser.add_argument("--workers", type=int, default=3)
    args = parser.parse_args()
    if args.api != "http://127.0.0.1:5263" or urlsplit(args.api).hostname != "127.0.0.1":
        parser.error("Only the existing isolated localhost 5263 API is accepted")
    if not 1 <= args.workers <= 4:
        parser.error("workers must be 1..4")
    args.output_dir.mkdir(parents=True, exist_ok=True)
    report_path = args.output_dir/"live-holdout-report.json"
    if report_path.exists():
        parser.error("Refusing to overwrite a completed report")
    cache = args.output_dir/"responses"
    cache.mkdir(exist_ok=True)
    manifest, projects, totals = load_source(args.source)
    settings = api_json(args.api, "GET", "/api/settings")["settings"]
    cases_before = api_json(args.api, "GET", "/api/analysis-cases")
    signature = digest({"source": manifest, "settings": settings, "api": args.api})
    tasks = [(p, scope) for p in projects.values() if p["split"] in ("validation", "test")
             for scope in SCOPES if totals[(p["synthetic_project_id"], scope)]["actual"] > 0]
    records = []
    with ThreadPoolExecutor(max_workers=args.workers) as executor:
        futures = [executor.submit(collect_one, p, scope, args.api, cache, signature, projects, totals, settings) for p, scope in tasks]
        for future in as_completed(futures):
            records.append(future.result())
            if len(records)%20 == 0 or len(records) == len(tasks):
                print(f"Validated live baseline + reference pool {len(records)}/{len(tasks)}", flush=True)
    records.sort(key=lambda r: (r["project_id"], r["scope"]))
    validation = [r for r in records if projects[r["project_id"]]["split"] == "validation"]
    test = [r for r in records if projects[r["project_id"]]["split"] == "test"]
    comparison = [evaluate(validation, cfg, settings, projects, totals) for cfg in configs(settings)]
    selected = select_validation(comparison)
    test_current = evaluate(test, baseline_config(settings), settings, projects, totals, live=True)
    test_selected = evaluate(test, selected, settings, projects, totals)
    after_settings = api_json(args.api, "GET", "/api/settings")["settings"]
    after_cases = api_json(args.api, "GET", "/api/analysis-cases")
    if digest(settings) != digest(after_settings) or digest(cases_before) != digest(after_cases):
        raise ValueError("API settings/case snapshot changed during the experiment")
    report = {"origin": "SYNTHETIC", "benchmark": "LIVE_RANGE_API_BASELINE_WITH_VALIDATED_COUNTERFACTUAL_REPLAY",
              "created_utc": datetime.now(timezone.utc).isoformat(), "source_manifest": manifest,
              "split_counts": dict(Counter(p["split"] for p in projects.values())),
              "settings_snapshot": settings, "api": args.api, "request_pairs_validated": len(records),
              "created_cases": 0, "created_runs": 0, "settings_changed": False,
              "case_snapshot_unchanged": True, "src_reference_policy": "SRC_ONLY",
              "validation_selection": "Minimum macro relative interval score over unshocked nonextreme validation observations; no test-based parameter selection.",
              "normal_label_status": "NO_ENGINEERING_NORMAL_CERTIFICATION; unshocked is a source condition, not engineering truth.",
              "anomaly_precision": None, "anomaly_recall": None, "anomaly_fpr": None,
              "anomaly_metric_blocker": "Available labels indicate injected multipliers/design variations; baselines lack certified engineering normal/abnormal labels. Do not substitute baseline for true negative.",
              "validation_comparison": [{k: v for k, v in c.items() if k != "details"} for c in comparison],
              "selected_on_validation": selected, "test_current": test_current, "test_selected": test_selected,
              "limitations": ["Synthetic numeric errors are not company engineering accuracy.",
                              "Existing C# range endpoint is exercised; workbook parsing/UI inclusion and history publication are not rerun.",
                              "Reference lineage is checked against source IDs, metadata and numeric totals, not a new Docker connection attestation.",
                              "Current service permits reference projects later than the target date; this is retrospective held-out estimation, not a chronological forecast.",
                              "Counterfactuals use returned eligible reference pools and inspected C# rules; only the current baseline is executed by the live service.",
                              "MAE is grouped by scope and structure; never average incompatible physical units."]}
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({"report": str(report_path), "validated_pairs": len(records), "selected": selected}, ensure_ascii=True), flush=True)


if __name__ == "__main__":
    main()
