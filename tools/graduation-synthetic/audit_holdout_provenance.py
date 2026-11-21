#!/usr/bin/env python3
"""Read-only source-lineage audit for synthetic held-out quantity/price baselines.

A row tagged "baseline" means that generator's deliberate .45/1.8 group-level
multiplier was not applied. It does NOT establish an engineering normal label.
Never uploads, re-seeds, alters scope, or computes a real-world FPR.
"""
import argparse
import csv
import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path

MATERIAL_SCOPE = {"concrete": "SYN-CONCRETE", "rebar": "SYN-REBAR"}
STRUCTURAL = "結構體工程"


def csv_records(path):
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def audit(source, paired_report=None):
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("data_origin") != "SYNTHETIC" or manifest.get("generator_version") != "2.0.0":
        raise ValueError("SYNTHETIC v2 manifest required")
    filenames = ("projects.csv", "historical_transactions.csv", "project_boq_rows.csv")
    for filename in filenames:
        actual = hashlib.sha256((source / filename).read_bytes()).hexdigest()
        if actual != manifest["files_sha256"][filename]:
            raise ValueError("source SHA256 mismatch: " + filename)
    projects = csv_records(source / "projects.csv")
    history = csv_records(source / "historical_transactions.csv")
    boq = csv_records(source / "project_boq_rows.csv")
    if (len(projects), len(history), len(boq)) != (
            manifest["project_count"], manifest["transaction_count"], manifest["boq_line_count"]):
        raise ValueError("source record counts mismatch")

    tx_by_id = {x["synthetic_transaction_id"]: x for x in history}
    if len(tx_by_id) != len(history):
        raise ValueError("duplicate transaction ID")
    status_by_project = defaultdict(lambda: {
        "line_counts": Counter(), "shocks": Counter(), "structural_lines": 0,
        "structural_shocks": 0,
    })
    for row in boq:
        tx = tx_by_id.get(row["synthetic_transaction_id"])
        if not tx or tx["synthetic_project_id"] != row["synthetic_project_id"] or tx["material_code"] != row["material_code"]:
            raise ValueError("BOQ/history lineage mismatch")
        label = tx.get("synthetic_label")
        if label not in ("baseline", "injected_anomaly"):
            raise ValueError("unexpected synthetic_label: " + str(label))
        pid = row["synthetic_project_id"]
        mat = row["material_code"]
        source_status = status_by_project[pid]
        source_status["line_counts"][mat] += 1
        if label == "injected_anomaly":
            source_status["shocks"][mat] += 1
        if row["major_category"] == STRUCTURAL:
            source_status["structural_lines"] += 1
            if label == "injected_anomaly":
                source_status["structural_shocks"] += 1

    project_rows = []
    for project in projects:
        if project["split"] != "test":
            continue
        pid = project["synthetic_project_id"]
        counts = status_by_project[pid]
        scopes = {}
        for label, mat in MATERIAL_SCOPE.items():
            total = counts["line_counts"][mat]
            shocks = counts["shocks"][mat]
            scopes[label] = {
                "applicable": total > 0,
                "sourceLineCount": total,
                "generatorShockedLineCount": shocks,
                "noRecordedGeneratorShock": total > 0 and shocks == 0,
                "engineeringNormalCertified": False,
            }
        total, shocks = counts["structural_lines"], counts["structural_shocks"]
        scopes["structural_price"] = {
            "applicable": total > 0, "sourceLineCount": total,
            "generatorShockedLineCount": shocks,
            "noRecordedGeneratorShock": total > 0 and shocks == 0,
            "engineeringNormalCertified": False,
        }
        project_rows.append({
            "projectId": pid, "structureType": project["structure_type"],
            "split": "test", "extremeScenario": project.get("extreme_scenario") or None,
            "scopes": scopes,
        })
    # A recorded shock is a provenance warning; absence of a shock does not
    # certify engineering plausibility or the intended design's normality.
    by_id = {p["projectId"]: p for p in project_rows}
    counts = {}
    for struct in ("RC", "SC", "SRC"):
        rows = [p for p in project_rows if p["structureType"] == struct]
        counts[struct] = {
            "testProjectCount": len(rows),
            "nonExtremeProjectCount": sum(not x["extremeScenario"] for x in rows),
            "scopes": {
                key: {
                    "applicable": sum(x["scopes"][key]["applicable"] for x in rows),
                    "withoutRecordedShock": sum(x["scopes"][key]["noRecordedGeneratorShock"] for x in rows),
                    "withRecordedShock": sum(x["scopes"][key]["generatorShockedLineCount"] > 0 for x in rows),
                } for key in ("concrete", "rebar", "structural_price")
            },
        }

    paired = []
    if paired_report is not None:
        doc = json.loads(paired_report.read_text(encoding="utf-8"))
        if doc.get("source") != "LOCAL_REAL_ASPNET_API" or doc.get("data_origin") != "SYNTHETIC":
            raise ValueError("not a synthetic real API paired report")
        for item in doc["results"]:
            if item.get("scenario") != "baseline":
                continue
            pid = item["baseProjectId"]
            if pid not in by_id:
                raise ValueError("paired baseline is not a test project: " + pid)
            source_row = by_id[pid]
            red = {}
            for scope, item_name in (("concrete", "混凝土"), ("rebar", "鋼筋")):
                value = item.get("quantities", {}).get(item_name)
                if value:
                    red[scope] = {
                        "observedStatus": value["analysisStatus"],
                        "generatorShockedLineCount": source_row["scopes"][scope]["generatorShockedLineCount"],
                        "engineeringNormalCertified": False,
                    }
            amount = item.get("price", {})
            if amount:
                red["structural_price"] = {
                    "observedStatus": amount.get("analysisStatus"),
                    "generatorShockedLineCount": source_row["scopes"]["structural_price"]["generatorShockedLineCount"],
                    "engineeringNormalCertified": False,
                }
            paired.append({"projectId": pid, "structureType": source_row["structureType"], "observedApi": red})
    return {
        "dataOrigin": "SYNTHETIC",
        "auditKind": "READ_ONLY_GENERATOR_PROVENANCE_NOT_NORMAL_TRUTH",
        "historyModified": False,
        "readyForFalsePositiveRate": False,
        "testProjectCount": len(project_rows),
        "byStructure": counts,
        "pairedBaselines": paired,
        "projects": project_rows,
        "limitations": [
            "No recorded generator multiplier does not establish engineering-normal design or unit price.",
            "A recorded multiplier is generator provenance, not proof that the whole project is abnormal.",
            "Actual API red lights on unlabeled baselines cannot be counted as false positives.",
            "Projects sharing a source baseline are correlated; exclude non-applicable categories.",
        ],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--paired-report", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error("refusing to overwrite existing audit")
    result = audit(args.source, args.paired_report)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "status": "PROVENANCE_AUDITED_NOT_ACCURACY",
        "testProjectCount": result["testProjectCount"],
        "pairedBaselines": len(result["pairedBaselines"]),
        "readyForFalsePositiveRate": False,
        "output": str(args.output),
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
