#!/usr/bin/env python3
"""Export independent SRC + extreme-scenario XLSX targets from a v2 synthetic dataset.

Never imports TEST data into history; refuses overwrite and verifies source SHA-256.
"""
import argparse
import csv
import json
from collections import Counter
from pathlib import Path

from export_full_project_excel import export, read_source


def plan(source):
    projects, boq = read_source(source)
    by_id = Counter(x["synthetic_project_id"] for x in boq)
    test = [p for p in projects if p["split"] == "test" and by_id[p["synthetic_project_id"]] > 0]
    src = next((p for p in test if p["structure_type"] == "SRC"
                and not p.get("extreme_scenario")), None)
    if src is None:
        raise ValueError("no ordinary test-only SRC project (use --include-src, --count >= 120)")
    result = [src]
    for scenario in ("tiny_lowrise", "large_highrise", "deep_basement",
                     "constrained_site", "wide_lowrise", "older_remote_city"):
        p = next((p for p in test if p.get("extreme_scenario") == scenario), None)
        if p is None:
            raise ValueError("missing test-only scenario: " + scenario)
        result.append(p)
    if len({p["synthetic_project_id"] for p in result}) != len(result):
        raise ValueError("duplicate holdout projects")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    if args.output_dir.exists() and any(args.output_dir.iterdir()):
        parser.error("output directory must be empty")
    try:
        chosen = plan(args.source)
        args.output_dir.mkdir(parents=True, exist_ok=True)
        summary = []
        for p in chosen:
            tag = p.get("extreme_scenario") or "src_baseline"
            prefix = args.output_dir / (tag + "_" + p["synthetic_project_id"])
            result = export(args.source, prefix.with_suffix(".xlsx"),
                            Path(str(prefix) + "-truth.json"),
                            p["synthetic_project_id"])
            summary.append({"scenario": tag, "project": p["synthetic_project_id"],
                            "structure_type": p["structure_type"],
                            "city": p["city"], "site_area": p["site_area"],
                            "split": p["split"], "excel": prefix.name + ".xlsx",
                            "truth": prefix.name + "-truth.json", **result})
        path = args.output_dir / "suite_manifest.json"
        path.write_text(json.dumps({"data_origin": "SYNTHETIC", "split": "test",
            "historical_import": False, "approval": "RESEARCH_ONLY",
            "projects": summary}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({"test_projects": len(summary),
            "structures": sorted({p["structure_type"] for p in summary}),
            "output_dir": str(args.output_dir)}, ensure_ascii=False))
    except (ValueError, OSError, KeyError) as exc:
        parser.error(str(exc))


if __name__ == "__main__":
    main()
