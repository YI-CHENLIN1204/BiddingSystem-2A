#!/usr/bin/env python3
"""Read-only city/site-area sensitivity smoke test against the *actual* 2A price API.

This does not write analysis cases, seeds, or transactions. Synthetic fixtures only.
A PASS shows a change in returned reference ranking/score, not predictive accuracy.
"""
import argparse
import csv
import json
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


def load_src_test(source):
    with (source / "projects.csv").open(encoding="utf-8-sig", newline="") as fp:
        projects = list(csv.DictReader(fp))
    return next((p for p in projects if p["split"] == "test"
                 and p["structure_type"] == "SRC"
                 and float(p["site_area"]) > 0), None)


def compare(base_url, target, city, site_area):
    changed = dict(target)
    changed["city"] = city
    changed["siteArea"] = site_area
    body = json.dumps({"target": changed, "majorCategory": "結構體工程",
                       "topCount": 8}).encode("utf-8")
    req = Request(base_url.rstrip("/") + "/api/major-category-price/range",
                  data=body, headers={"Content-Type": "application/json; charset=utf-8"},
                  method="POST")
    try:
        with urlopen(req, timeout=60) as resp:
            result = json.loads(resp.read().decode("utf-8"))
    except HTTPError as error:
        raise RuntimeError(f"HTTP {error.code}: {error.read().decode('utf-8', errors='replace')}") from error
    except URLError as error:
        raise RuntimeError(f"API connection failed: {error.reason}") from error
    refs = result.get("references", [])
    if not refs:
        raise RuntimeError("API returned no reference projects; cannot test sensitivity")
    return {
        "referenceProjectCount": result.get("referenceProjectCount"),
        "availableProjectCount": result.get("availableProjectCount"),
        "scores": [(r["projectId"], r["similarityScore"]) for r in refs],
        "hasEnoughSamples": result.get("hasEnoughSamples"),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--api", default="http://127.0.0.1:5261")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error("refusing overwrite")
    p = load_src_test(args.source)
    if p is None:
        parser.error("no test-only SRC project with valid site area")
    base = {"projectType": p["project_type"], "structureType": p["structure_type"],
            "city": p["city"], "totalFloorArea": float(p["total_floor_area"]),
            "siteArea": float(p["site_area"]),
            "floorCount": int(p["floor_count"]),
            "basementFloorCount": int(p["basement_floor_count"]),
            "projectDate": p["start_date"]}
    new_city = "臺北市" if base["city"] != "臺北市" else "高雄市"
    normal = compare(args.api, base, base["city"], base["siteArea"])
    city = compare(args.api, base, new_city, base["siteArea"])
    site = compare(args.api, base, base["city"], base["siteArea"] * 3)
    city_changed = city["scores"] != normal["scores"]
    site_changed = site["scores"] != normal["scores"]
    output = {
        "data_origin": "SYNTHETIC",
        "project_id": p["synthetic_project_id"],
        "source_split": "test",
        "base_city": base["city"],
        "alternate_city": new_city,
        "base_site_area": base["siteArea"],
        "alternate_site_area": base["siteArea"] * 3,
        "city_scores_changed": city_changed,
        "site_area_scores_changed": site_changed,
        "status": "PASS" if city_changed and site_changed else "INVESTIGATE",
        "note": "Sensitivity check only. No accuracy claims; reference rankings can change after parameter updates.",
        "baseline": normal, "city_alternative": city, "site_alternative": site,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n",
                           encoding="utf-8")
    print(json.dumps({k: output[k] for k in (
        "project_id", "city_scores_changed", "site_area_scores_changed", "status")},
        ensure_ascii=False))
    if output["status"] != "PASS":
        raise SystemExit(2)


if __name__ == "__main__":
    main()
