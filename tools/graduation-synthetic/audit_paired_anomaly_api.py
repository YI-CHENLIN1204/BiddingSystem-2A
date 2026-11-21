#!/usr/bin/env python3
"""Pairwise real 2A API smoke audit of held-out synthetic XLSX files.

Creates analysis-case snapshots and analysis runs in the selected LOCAL test DB.
Never seeds history, modifies mappings, or promotes red/green scope.
Baseline red is not automatically a false positive: the source generator
contains natural variability and some random group-level stress multipliers.
"""
import argparse
import hashlib
import json
import sys
import uuid
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import Request, urlopen

def request_json(method, url, value=None, timeout=180):
    headers = {}
    body = None
    if value is not None:
        body = json.dumps(value, ensure_ascii=False).encode("utf-8")
        headers["Content-Type"] = "application/json; charset=utf-8"
    try:
        with urlopen(Request(url, data=body, headers=headers, method=method), timeout=timeout) as response:
            return json.loads(response.read().decode("utf-8"))
    except HTTPError as exc:
        raise RuntimeError(f"{method} {url} HTTP {exc.code}: {exc.read().decode('utf-8', errors='replace')[:1200]}") from exc
    except URLError as exc:
        raise RuntimeError(f"{method} {url}: {exc.reason}") from exc


def upload_case(api, case_name, workbook, truth):
    project = truth["project"]
    if project["split"] != "test":
        raise ValueError("refusing non-test workbook upload")
    boundary = "2a-" + uuid.uuid4().hex
    fields = {
        "caseName": case_name,
        "projectType": project["project_type"],
        "structureType": project["structure_type"],
        "city": project["city"],
        "totalFloorArea": project["total_floor_area"],
        "siteArea": project["site_area"],
        "floorCount": project["floor_count"],
        "basementFloorCount": project["basement_floor_count"],
        "projectDate": project["start_date"],
        "includedSheetNames": json.dumps(list(truth["detail_rows_per_sheet"]), ensure_ascii=False),
    }
    chunks = []
    for key, value in fields.items():
        chunks.append((f"--{boundary}\r\nContent-Disposition: form-data; name=\"{key}\"\r\n\r\n{value}\r\n").encode("utf-8"))
    chunks.append((f"--{boundary}\r\nContent-Disposition: form-data; name=\"excelFile\"; filename=\"{workbook.name}\"\r\nContent-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\r\n\r\n").encode("utf-8"))
    chunks.append(workbook.read_bytes())
    chunks.append(f"\r\n--{boundary}--\r\n".encode("ascii"))
    request = Request(api + "/api/analysis-cases/from-upload", data=b"".join(chunks),
                      headers={"Content-Type": "multipart/form-data; boundary=" + boundary},
                      method="POST")
    try:
        with urlopen(request, timeout=300) as response:
            result = json.loads(response.read().decode("utf-8"))
    except HTTPError as exc:
        raise RuntimeError(f"upload {workbook.name} HTTP {exc.code}: {exc.read().decode('utf-8', errors='replace')[:1200]}") from exc
    case_id, version_id = result.get("analysisCaseId"), result.get("analysisBidVersionId")
    if not isinstance(case_id, int) or case_id <= 0 or not isinstance(version_id, int) or version_id <= 0:
        raise RuntimeError(f"upload did not return positive IDs: {result}")
    if result.get("itemCount") != truth["total_detail_rows"]:
        raise RuntimeError(f"import mismatch {workbook.name}: actual={result.get('itemCount')} expected={truth['total_detail_rows']}")
    return result


def check_inputs(folder):
    manifest = json.loads((folder / "anomaly_suite_manifest.json").read_text(encoding="utf-8"))
    if manifest.get("data_origin") != "SYNTHETIC" or manifest.get("test_only") is not True or manifest.get("scenario_count") != 10:
        raise ValueError("not an approved test-only 10-case synthetic suite")
    records = []
    baselines = {}
    for base in manifest["baseline_workbooks"]:
        baselines[base["base_project_id"]] = base
        records.append(("baseline", base["base_project_id"], base))
    for case in manifest["scenarios"]:
        if case["base_project_id"] not in baselines or case["split"] != "test":
            raise ValueError("missing paired baseline or non-test scenario")
        records.append((case["scenario"], case["base_project_id"], case))
    if len(records) != 13 or len(baselines) != 3:
        raise ValueError("expected exactly 3 baselines and 10 scenarios")
    checked = []
    for scenario, pid, item in records:
        xlsx, truth = folder / item["xlsx"], folder / item["truth"]
        if not xlsx.is_file() or not truth.is_file():
            raise ValueError("missing workbook/truth: " + str(xlsx))
        doc = json.loads(truth.read_text(encoding="utf-8"))
        if doc["data_origin"] != "SYNTHETIC" or doc["project"]["split"] != "test" or doc["project"]["synthetic_project_id"] != pid:
            raise ValueError("truth lineage invalid: " + str(truth))
        if hashlib.sha256(xlsx.read_bytes()).hexdigest() != doc["xlsx_sha256"]:
            raise ValueError("XLSX sha256 mismatch: " + str(xlsx))
        if (scenario == "baseline") != (doc.get("injection") is None):
            raise ValueError("injection label mismatch: " + scenario)
        if scenario != "baseline" and doc["injection"]["scenario"] != scenario:
            raise ValueError("scenario label mismatch: " + scenario)
        checked.append((scenario, pid, item, xlsx, doc))
    return checked


def summarize(name, source_id, upload, quantity, price):
    qty = quantity.get("result")
    amount = price.get("result")
    if not isinstance(qty, dict) or not isinstance(amount, dict):
        raise RuntimeError("analysis API returned unexpected envelope")
    by_category = {}
    for item in qty.get("items", []):
        category = item.get("detectionCategoryName")
        if category in ("鋼筋", "混凝土"):
            by_category[category] = {
                "observedQuantity": item.get("observedQuantity"),
                "unit": item.get("unitName"),
                "lowerQuantity": (item.get("range") or {}).get("lowerQuantity"),
                "upperQuantity": (item.get("range") or {}).get("upperQuantity"),
                "analysisStatus": item.get("analysisStatus"),
                "trafficLight": item.get("trafficLight"),
            }
    return {
        "scenario": name, "baseProjectId": source_id,
        "analysisCaseId": upload["analysisCaseId"],
        "versionId": upload["analysisBidVersionId"],
        "parsedItemCount": upload["itemCount"],
        "quantityRunId": quantity.get("analysisRunId"),
        "priceRunId": price.get("analysisRunId"),
        "quantities": by_category,
        "price": {
            "observedAmount": amount.get("observedAmount"),
            "lowerAmount": amount.get("lowerAmount"),
            "upperAmount": amount.get("upperAmount"),
            "analysisStatus": amount.get("analysisStatus"),
            "trafficLight": amount.get("trafficLight"),
            "referenceProjectCount": amount.get("referenceProjectCount"),
        },
    }


def pairs(rows):
    baseline = {row["baseProjectId"]: row for row in rows if row["scenario"] == "baseline"}
    output = []
    for row in rows:
        if row["scenario"] == "baseline":
            continue
        base = baseline.get(row["baseProjectId"])
        if not base:
            continue
        qdelta = {}
        for name in ("鋼筋", "混凝土"):
            left, right = base["quantities"].get(name), row["quantities"].get(name)
            if left and right and left["observedQuantity"] is not None and right["observedQuantity"] is not None:
                qdelta[name] = {
                    "observedDelta": round(right["observedQuantity"] - left["observedQuantity"], 4),
                    "baselineLight": left["trafficLight"],
                    "variantLight": right["trafficLight"],
                    "baselineStatus": left["analysisStatus"],
                    "variantStatus": right["analysisStatus"],
                }
        lb, rb = base["price"], row["price"]
        output.append({
            "scenario": row["scenario"], "baseProjectId": row["baseProjectId"],
            "quantities": qdelta,
            "priceObservedDelta": (
                round(rb["observedAmount"] - lb["observedAmount"], 2)
                if lb["observedAmount"] is not None and rb["observedAmount"] is not None else None),
            "baselinePriceLight": lb["trafficLight"],
            "variantPriceLight": rb["trafficLight"],
            "baselinePriceStatus": lb["analysisStatus"],
            "variantPriceStatus": rb["analysisStatus"],
            "note": "Directional comparison only, not detection accuracy or engineering compliance",
        })
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--suite", type=Path, required=True)
    parser.add_argument("--api", default="http://127.0.0.1:5262")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--execute", action="store_true", help="explicitly create 13 analysis cases and 26 analysis runs in local DB")
    args = parser.parse_args()
    parsed = urlsplit(args.api)
    if parsed.scheme != "http" or parsed.hostname not in ("127.0.0.1", "localhost"):
        parser.error("refusing non-local API")
    if args.output.exists() or Path(str(args.output) + ".partial.json").exists():
        parser.error("refusing to overwrite existing report/partial")
    records = check_inputs(args.suite)
    if not args.execute:
        print(json.dumps({"status": "DRY_RUN", "verifiedXlsxAndTruth": len(records),
                          "api": args.api, "next": "run again with --execute to create 13 analysis cases"},
                         ensure_ascii=False))
        return
    api = args.api.rstrip("/")
    quantity_ready = request_json("GET", api + "/api/detection-category/readiness")
    price_ready = request_json("GET", api + "/api/major-category-price/readiness")
    if quantity_ready.get("readyCount", 0) < 2 or price_ready.get("status") != "ReadyForEstimation":
        raise RuntimeError("quantity/price history not ready; refusing to create cases")
    rows = []
    partial = Path(str(args.output) + ".partial.json")
    for index, (name, pid, item, xlsx, truth) in enumerate(records, 1):
        print(f"[{index}/{len(records)}] {name} {pid}", flush=True)
        uploaded = upload_case(api, f"SYNTHETIC PAIR {name} {pid}", xlsx, truth)
        case_id, version_id = uploaded["analysisCaseId"], uploaded["analysisBidVersionId"]
        path = f"{api}/api/analysis-cases/{case_id}/versions/{version_id}"
        try:
            quantity = request_json("POST", path + "/quantity-analysis", {})
            price = request_json("POST", path + "/major-price-analysis",
                                 {"majorCategory": "結構體工程", "topCount": 8})
            rows.append(summarize(name, pid, uploaded, quantity, price))
        except Exception:
            # Preserve identifiers even if subsequent analysis fails.
            rows.append({"scenario": name, "baseProjectId": pid, "analysisCaseId": case_id,
                         "versionId": version_id, "status": "ANALYSIS_FAILED"})
            partial.write_text(json.dumps({"completed": rows}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            raise
        partial.write_text(json.dumps({"completed": rows}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    report = {
        "data_origin": "SYNTHETIC",
        "source": "LOCAL_REAL_ASPNET_API",
        "history_import": False,
        "formal_scope_promotion": False,
        "baseline_count": 3,
        "variant_count": 10,
        "summary_note": "All variants share one of three base projects; synthetic cases are not independent accuracy observations.",
        "results": rows, "paired_deltas": pairs(rows),
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    partial.unlink()
    print(json.dumps({"status": "API_COMPLETED_NOT_ACCURACY_PASS",
                      "baselines": 3, "variants": 10,
                      "report": str(args.output)}, ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, RuntimeError, KeyError, TypeError) as exc:
        print("ERROR: " + str(exc), file=sys.stderr)
        sys.exit(2)
