#!/usr/bin/env python3
"""Prepare safe synthetic UI workbooks, standalone or from verified holdout source.

Without --source this script requires NO local dataset and NO running API. The
standalone synthetic workbook is strictly a UI smoke fixture, NOT a test
holdout, reference history or measured engineering quantity/price.
"""
import argparse
import hashlib
import json
import urllib.error
import urllib.request
from pathlib import Path
from decimal import Decimal

from generate_irregular_excel import make_xlsx

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



def create_standalone_fixture(output, truth, structure="SRC"):
    """Produce deterministic multi-sheet UI smoke fixture; no DB or API access.

    Distinct output filenames create distinct case names. All numbers are
    illustrative, not derived from actual construction projects or historic
    training/holdout samples. This path never calls POST or publishes history.
    """
    if output.exists() or truth.exists() or output.resolve() == truth.resolve():
        raise ValueError("Existing output/truth files will never be overwritten")
    if structure not in ("SRC", "RC"):
        raise ValueError("structure must be SRC or RC")
    headers = ["項次", "工項名稱", "規格", "單位", "數量", "單價", "金額", "工程大項"]
    # Illustration-only amounts. Each line has a positive quantity and price.
    structural = [
        ("SD420W D19 鋼筋加工及組立", "D19", "kg", 42000, 39, "結構體工程"),
        ("SD420W D22 鋼筋加工及組立", "D22", "kg", 32000, 40, "結構體工程"),
        ("基礎鋼筋綁紮材料及工資", "D16", "kg", 18000, 42, "結構體工程"),
        ("柱梁主筋鋼筋施工", "D25", "kg", 38000, 43, "結構體工程"),
        ("預拌混凝土澆置", "280kgf/cm2", "m3", 1800, 2600, "結構體工程"),
        ("樓板混凝土泵送與搗實", "350kgf/cm2", "m3", 1450, 2850, "結構體工程"),
        ("基礎底版預拌混凝土", "280kgf/cm2", "m3", 1150, 2750, "結構體工程"),
        ("柱梁牆預拌混凝土", "350kgf/cm2", "m3", 1050, 2900, "結構體工程"),
        ("普通模板組立與拆除", "柱梁版", "m2", 10500, 620, "結構體工程"),
        ("樓板支撐系統租賃", "含安裝", "m2", 5400, 450, "結構體工程"),
    ]
    if structure == "SRC":
        structural.extend([
            ("H型鋼構梁柱製作及吊裝", "H-300", "kg", 105000, 91, "鋼骨鋼構工程"),
            ("鋼骨接合板及現場組立", "SRC", "kg", 38000, 98, "鋼骨鋼構工程"),
        ])
    earthworks = [
        ("基礎土方開挖及外運", "一般土", "m3", 4300, 320, "土方及基礎工程"),
        ("地下室擋土支撐組立", "臨時支撐", "式", 2, 175000, "土方及基礎工程"),
        ("地下室防水塗膜施作", "PU", "m2", 2800, 610, "防水工程"),
    ]
    def make_detail(rows):
        body = [["SYNTHETIC 測試明細（非決標標單）"], [],
                headers, ["壹", "工程明細"]]
        audit = []
        for index, (name, spec, unit, quantity, unit_price, major) in enumerate(rows, 1):
            amount = Decimal(quantity) * Decimal(unit_price)
            body.append([str(index), name, spec, unit, quantity, unit_price,
                         amount, major])
            audit.append({
                "itemName": name, "spec": spec, "unit": unit,
                "quantity": quantity, "unitPrice": unit_price,
                "amount": str(amount), "majorCategory": major,
            })
        return body, audit

    structure_sheet, structure_truth = make_detail(structural)
    site_sheet, site_truth = make_detail(earthworks)
    sheets = {
        "工程總表": [
            ["SYNTHETIC-UI 單純功能測試，不得發布為歷史基準"],
            ["結構型式", structure],
            ["說明", "工程面積、數量、單價皆非真實工程參考值"],
        ],
        "結構體明細": structure_sheet,
        "基礎及地下工程": site_sheet,
    }
    blob = make_xlsx(sheets, headers_by_sheet={
        "結構體明細": headers, "基礎及地下工程": headers,
    })
    project_id = "UI-" + structure + "-" + output.stem
    project = {
        "synthetic_project_id": project_id,
        "project_name": "SYNTHETIC-UI-驗收-" + output.stem,
        "project_type": "住宅",
        "structure_type": structure,
        "city": "臺中市",
        "total_floor_area": "12000",
        "site_area": "950",
        "floor_count": "12",
        "basement_floor_count": "2",
        "start_date": "2025-01-01",
        "data_origin": "SYNTHETIC_STANDALONE_UI",
        "split": "ui-smoke-not-holdout",
    }
    metadata = {
        "data_origin": "SYNTHETIC_STANDALONE_UI",
        "source_split": "ui-smoke-not-holdout",
        "eligible_for_training_history": False,
        "is_accuracy_ground_truth": False,
        "project": project,
        "xlsx_sha256": hashlib.sha256(blob).hexdigest(),
        "total_detail_rows": len(structure_truth) + len(site_truth),
        "detail_rows_per_sheet": {
            "結構體明細": len(structure_truth),
            "基礎及地下工程": len(site_truth),
        },
        "cases": structure_truth + site_truth,
        "limitations": [
            "Standalone UI smoke test only; no historical reference dataset.",
            "Illustrative non-engineering quantities and prices.",
            "Never upload as final bid or publish Approved historical data.",
        ],
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    truth.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(blob)
    truth.write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + "\n",
                     encoding="utf-8")
    return {
        "status": "SYNTHETIC_UI_ONLY_NOT_HOLDOUT_OR_HISTORY",
        "file": str(output),
        "truth": str(truth),
        "caseName": project["project_name"],
        "projectType": project["project_type"],
        "structureType": structure,
        "city": project["city"],
        "totalFloorArea": project["total_floor_area"],
        "siteArea": project["site_area"],
        "floorCount": project["floor_count"],
        "basementFloorCount": project["basement_floor_count"],
        "projectMonth": project["start_date"][:7],
        "projectId": project_id,
        "worksheetCount": len(sheets),
        "detailRows": metadata["total_detail_rows"],
        "instructions": "Use ONLY /analysis-cases self-upload. Never publish this workbook to Approved history.",
    }

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, help="Optional verified v2 synthetic source. Omit for standalone UI smoke Excel.")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--truth", type=Path, required=True)
    parser.add_argument("--structure", choices=("SRC", "RC"), default="SRC")
    parser.add_argument("--api", default="http://127.0.0.1:5263")
    args = parser.parse_args()
    try:
        if args.output.exists() or args.truth.exists():
            raise ValueError("Existing output/truth files will never be overwritten")
        if args.source is None:
            print(json.dumps(create_standalone_fixture(args.output, args.truth, args.structure),
                             ensure_ascii=False, indent=2))
            return
        if not (args.source / "manifest.json").is_file():
            raise ValueError(f"Missing source manifest: {args.source / 'manifest.json'}. Omit --source for offline UI smoke fixture.")
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
