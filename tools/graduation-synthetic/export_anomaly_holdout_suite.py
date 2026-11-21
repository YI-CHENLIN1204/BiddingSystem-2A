#!/usr/bin/env python3
"""Generate labeled quantity and price anomalies in TEST-only workbooks.

Train/validation/history are read-only; variants of one source project are
correlated stress scenarios, not independent observations for accuracy metrics.
"""
import argparse
import json
from collections import defaultdict
from pathlib import Path

from export_full_project_excel import read_source, export

# Scenario, source structure, required material.
CASES = [
    ("design_concrete_high", "RC", "SYN-CONCRETE"),
    ("design_concrete_low", "SRC", "SYN-CONCRETE"),
    ("design_rebar_high", "RC", "SYN-REBAR"),
    ("design_rebar_low", "SRC", "SYN-REBAR"),
    ("design_steel_high", "SC", "SYN-STEEL"),
    ("design_steel_low", "SRC", "SYN-STEEL"),
    ("structural_price_high", "SRC", "SYN-CONCRETE"),
    ("structural_price_low", "RC", "SYN-CONCRETE"),
    ("combined_concrete_high_price_high", "SRC", "SYN-CONCRETE"),
    ("combined_concrete_low_price_low", "RC", "SYN-CONCRETE"),
]


def plan(source):
    projects, boq = read_source(source)
    codes = defaultdict(set)
    for item in boq:
        codes[item["synthetic_project_id"]].add(item["material_code"])
    output = []
    for scenario, structure, material in CASES:
        project = next((p for p in projects
                        if p["split"] == "test" and
                        p["structure_type"] == structure and
                        not p.get("extreme_scenario") and
                        material in codes[p["synthetic_project_id"]]), None)
        if project is None:
            raise ValueError("no test-only baseline project with " + structure + "/" + material)
        output.append((scenario, project))
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    if args.output_dir.exists() and any(args.output_dir.iterdir()):
        parser.error("refusing to overwrite output directory")
    try:
        cases = plan(args.source)
        args.output_dir.mkdir(parents=True, exist_ok=True)
        summary = []
        # Pair every manipulated case with a zero-multiplier baseline workbook
        # from the SAME project; a variant is not an independent project.
        baselines = {}
        for _, project in cases:
            pid = project["synthetic_project_id"]
            if pid in baselines:
                continue
            name = "baseline_" + pid
            base = args.output_dir / name
            baseline_result = export(args.source, Path(str(base) + ".xlsx"),
                                     Path(str(base) + "-truth.json"), pid)
            baselines[pid] = {
                "base_project_id": pid, "xlsx": name + ".xlsx",
                "truth": name + "-truth.json", "detail_rows": baseline_result["detail_rows"]}
        for scenario, project in cases:
            prefix = args.output_dir / (scenario + "_" + project["synthetic_project_id"])
            xlsx = Path(str(prefix) + ".xlsx")
            truth = Path(str(prefix) + "-truth.json")
            result = export(args.source, xlsx, truth, project["synthetic_project_id"],
                            anomaly_scenario=scenario)
            truth_doc = json.loads(truth.read_text(encoding="utf-8"))
            assert truth_doc["injection"]["changed_line_count"] > 0
            summary.append({
                "scenario": scenario,
                "base_project_id": project["synthetic_project_id"],
                "structure_type": project["structure_type"],
                "split": "test",
                "xlsx": xlsx.name,
                "truth": truth.name,
                "injected_lines": result["changed_line_count"],
                "note": "Correlated variants; compare per scenario, never count as independent projects",
            })
        output = {
            "data_origin": "SYNTHETIC",
            "origin": "HELDOUT_ONLY_NOT_TRAINING_HISTORY",
            "test_only": True,
            "scenario_count": len(summary),
            "baseline_count": len(baselines),
            "baseline_workbooks": list(baselines.values()),
            "scenarios": summary,
            "ground_truth_definition": "Injected multipliers, not guaranteed model alarm correctness",
        }
        (args.output_dir / "anomaly_suite_manifest.json").write_text(
            json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({"scenarios": len(summary), "output_dir": str(args.output_dir)},
                         ensure_ascii=False))
    except (ValueError, KeyError, OSError) as error:
        parser.error(str(error))


if __name__ == "__main__":
    main()
