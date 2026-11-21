#!/usr/bin/env python3
"""Choose a fresh test-split synthetic BOQ for 2A UI upload, without DB writes."""
import argparse
import json
import urllib.error
import urllib.request
from pathlib import Path

from export_full_project_excel import export, read_source

# Baseline IDs already used by the paired suite; do not silently reuse.
PREVIOUS_BASELINES = {"SYN-00026", "SYN-00029", "SYN-00030"}


def get_case_names(api):
    if api != "http://127.0.0.1:5263":
        raise ValueError("Only existing localhost 5263 synthetic backend is supported")
    with urllib.request.urlopen(api + "/api/analysis-cases", timeout=15) as response:
        cases = json.load(response)
    if not isinstance(cases, list):
        raise ValueError("Case list is not an array")
    return [str(c.get("caseName", "")) for c in cases]


def select_project(projects, prior_names, structure):
    available = []
    for project in projects:
        project_id = project["synthetic_project_id"]
        if project.get("split") != "test" or project["structure_type"] != structure:
            continue
        if project_id in PREVIOUS_BASELINES:
            continue
        if any(project_id in name for name in prior_names):
            continue
        if float(project["total_floor_area"]) <= 0 or float(project.get("site_area", 0)) <= 0:
            continue
        available.append(project)
    if not available:
        raise ValueError("No unused test-split synthetic project with valid geometry")
    return sorted(available, key=lambda p: p["synthetic_project_id"])[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=Path("graduation-src-extreme-v3"))
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--truth", type=Path, required=True)
    parser.add_argument("--structure", choices=("SRC", "RC"), default="SRC")
    parser.add_argument("--api", default="http://127.0.0.1:5263")
    args = parser.parse_args()
    try:
        if args.output.exists() or args.truth.exists():
            raise ValueError("Existing output/truth files will never be overwritten")
        projects, _ = read_source(args.source)
        prior_names = get_case_names(args.api)
        selected = select_project(projects, prior_names, args.structure)
        result = export(args.source, args.output, args.truth,
                        project_id=selected["synthetic_project_id"])
        if result["project_id"] != selected["synthetic_project_id"]:
            raise ValueError("Exporter selected a different project")
        print(json.dumps({
            "status": "SYNTHETIC_TEST_ONLY_NOT_HISTORY",
            "file": str(args.output),
            "truth": str(args.truth),
            "caseName": "SYNTHETIC-UI-驗收-" + selected["synthetic_project_id"],
            "projectType": selected["project_type"],
            "structureType": selected["structure_type"],
            "city": selected["city"],
            "totalFloorArea": selected["total_floor_area"],
            "siteArea": selected["site_area"],
            "floorCount": selected["floor_count"],
            "basementFloorCount": selected["basement_floor_count"],
            "projectMonth": selected["start_date"][:7],
            "projectId": selected["synthetic_project_id"],
            "worksheetCount": result["worksheets"],
            "detailRows": result["detail_rows"],
            "instructions": "Use ONLY /analysis-cases self-upload. Do NOT upload this test holdout as a final bid or publish to history."
        }, ensure_ascii=False, indent=2))
    except (ValueError, KeyError, OSError, urllib.error.URLError, json.JSONDecodeError) as err:
        parser.error(str(err))


if __name__ == "__main__":
    main()
