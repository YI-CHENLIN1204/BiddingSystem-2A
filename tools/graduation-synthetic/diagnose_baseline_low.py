#!/usr/bin/env python3
"""Read-only diagnosis of existing synthetic baseline API runs.

GET existing run IDs; never create an AnalysisCase, AnalysisRun or history row.
A red light on unapproved synthetic "normal" is NOT a measured false positive.
"""
import argparse
import csv
import hashlib
import json
import re
import sys
from pathlib import Path
from urllib.parse import urlsplit
from urllib.request import urlopen

SCOPES = (("concrete", "混凝土"), ("rebar", "鋼筋"), ("structural_price", None))
SYN_NAME = re.compile(r"SYN-\d{5}")


def fetch_json(url):
    with urlopen(url, timeout=90) as response:
        return json.load(response)


def get_reference_metadata(source):
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("data_origin") != "SYNTHETIC" or manifest.get("generator_version") != "2.0.0":
        raise ValueError("unexpected source generator")
    path = source / "projects.csv"
    if hashlib.sha256(path.read_bytes()).hexdigest() != manifest["files_sha256"]["projects.csv"]:
        raise ValueError("projects.csv SHA mismatch")
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return {row["synthetic_project_id"]: row for row in csv.DictReader(handle)}


def detail_result(api, case_id, run_id):
    if not isinstance(case_id, int) or case_id <= 0 or not isinstance(run_id, int) or run_id <= 0:
        raise ValueError("invalid original analysis run ID")
    response = fetch_json(f"{api}/api/analysis-cases/{case_id}/runs/{run_id}")
    if response.get("analysisRunId") != run_id:
        raise ValueError("unexpected run ID from API")
    result = response.get("result")
    if not isinstance(result, dict):
        raise ValueError("missing stored result for run " + str(run_id))
    return result


def reference_digest(refs, expected_structure, metadata):
    output = []
    cross_structure = []
    unknown = []
    for ref in refs:
        name = str(ref.get("projectName", ""))
        m = SYN_NAME.search(name)
        pid = m.group(0) if m else None
        source = metadata.get(pid)
        structure = source.get("structure_type") if source else None
        item = {
            "project": pid,
            "structureType": structure,
            "similarityScore": ref.get("similarityScore"),
            "ageYears": ref.get("ageYears"),
            "finalWeight": ref.get("finalWeight"),
            "usagePerArea": ref.get("usagePerArea"),
            "pricePerPing": ref.get("pricePerPing"),
        }
        output.append(item)
        if structure is None:
            unknown.append(pid)
        elif structure != expected_structure:
            cross_structure.append(pid)
    return {"count": len(output), "crossStructure": cross_structure,
            "unresolvedReferenceIds": unknown, "references": output}


def margin(observed, lower, upper):
    if not all(isinstance(x, (int, float)) for x in (observed, lower, upper)) or lower <= 0 or upper < lower:
        raise ValueError("invalid original observation/range")
    return {
        "observed": observed, "lower": lower, "upper": upper,
        "belowLowerPercent": round(max(0, (lower - observed) / lower * 100), 3),
        "aboveUpperPercent": round(max(0, (observed - upper) / upper * 100), 3),
    }


def run_diagnosis(api, paired_path, provenance_path, source):
    paired = json.loads(paired_path.read_text(encoding="utf-8"))
    provenance = json.loads(provenance_path.read_text(encoding="utf-8"))
    if paired.get("source") != "LOCAL_REAL_ASPNET_API" or paired.get("data_origin") != "SYNTHETIC":
        raise ValueError("expected recorded synthetic API report")
    if provenance.get("auditKind") != "READ_ONLY_GENERATOR_PROVENANCE_NOT_NORMAL_TRUTH":
        raise ValueError("unexpected provenance report")
    if provenance.get("readyForFalsePositiveRate") is not False:
        raise ValueError("unsafe engineering-normal claim")
    metadata = get_reference_metadata(source)
    prov = {x["projectId"]: x for x in provenance["pairedBaselines"]}
    results = []
    for baseline in paired["results"]:
        if baseline.get("scenario") != "baseline":
            continue
        pid = baseline["baseProjectId"]
        if pid not in prov or pid not in metadata or metadata[pid]["split"] != "test":
            raise ValueError("baseline provenance or split mismatch: " + pid)
        expected_struct = metadata[pid]["structure_type"]
        if prov[pid]["structureType"] != expected_struct:
            raise ValueError("baseline structure mismatch: " + pid)
        quantity = detail_result(api, baseline["analysisCaseId"], baseline["quantityRunId"])
        price = detail_result(api, baseline["analysisCaseId"], baseline["priceRunId"])
        category_items = {i.get("detectionCategoryName"): i for i in quantity.get("items", [])}
        scopes = {}
        for scope, category in SCOPES:
            if scope == "structural_price":
                record = price
                observed, lower, upper = (
                    record.get("observedAmount"), record.get("lowerAmount"), record.get("upperAmount"))
                refs = record.get("references", [])
                summary = baseline["price"]
            else:
                record = category_items.get(category)
                summary = baseline["quantities"].get(category)
                if record is None or summary is None:
                    continue
                observed, lower, upper = (record.get("observedQuantity"),
                    (record.get("range") or {}).get("lowerQuantity"),
                    (record.get("range") or {}).get("upperQuantity"))
                refs = (record.get("range") or {}).get("references", [])
            if summary.get("analysisStatus") != record.get("analysisStatus"):
                raise ValueError("API stored run disagrees with saved summary: " + pid + "/" + scope)
            if abs(float(summary["observedAmount" if scope == "structural_price" else "observedQuantity"]) - float(observed)) > .02:
                raise ValueError("API observed value mismatch: " + pid + "/" + scope)
            scopes[scope] = {
                "status": record.get("analysisStatus"),
                "margin": margin(observed, lower, upper),
                "generatorShockedLineCount": prov[pid]["observedApi"][scope]["generatorShockedLineCount"],
                "references": reference_digest(refs, expected_struct, metadata),
                "engineeringNormalCertified": False,
            }
        results.append({"projectId": pid, "structureType": expected_struct, "scopes": scopes})
    if len(results) != 3:
        raise ValueError("expected exactly three previously recorded baselines")
    return {"status": "READ_ONLY_DIAGNOSTIC_NOT_ACCURACY",
            "source": "EXISTING_SAVED_ANALYSIS_RUNS",
            "createdRuns": 0, "historyModified": False,
            "readyForFalsePositiveRate": False, "baselines": results,
            "limits": [
                "Read-only run references reveal selection, not causal model errors.",
                "Cross-structure matches may be intentional: inspect model policy before labeling bug.",
                "Generator no-shock labels are not independently judged normal projects.",
                "Do not rerun the old 13-case --execute workflow.",
            ]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api", default="http://127.0.0.1:5262")
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--paired-report", type=Path, required=True)
    parser.add_argument("--provenance", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    url = urlsplit(args.api)
    if url.scheme != "http" or url.hostname not in ("127.0.0.1", "localhost"):
        parser.error("only local HTTP API allowed")
    if args.output.exists():
        parser.error("refusing to overwrite output")
    data = run_diagnosis(args.api.rstrip("/"), args.paired_report, args.provenance, args.source)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "status": data["status"], "baselines": len(data["baselines"]),
        "createdRuns": 0, "output": str(args.output)}, ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, TypeError, RuntimeError) as err:
        print("ERROR: " + str(err), file=sys.stderr)
        sys.exit(2)
