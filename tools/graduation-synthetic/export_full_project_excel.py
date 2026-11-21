#!/usr/bin/env python3
"""Export one coherent multi-trade synthetic BOQ, using the exact history rows.

The export source is a v2 dataset. No database activity or real project claims.
"""
import argparse
import csv
import hashlib
import json
import random
from collections import Counter, defaultdict
from pathlib import Path

from generate_irregular_excel import LAYOUTS, SHEETS, make_xlsx

# Explicit major categories supported by the current TwoAScopePolicy.
# Preserve the original finer trade in source CSV/truth, but the workbook
# includes a 1D-level classification required by AnalysisCase/from-upload.
APPROVED_MAJOR_CATEGORY = {
    "結構體工程": "結構體工程",
    "泥作工程": "內外牆裝修工程",
    "假設工程": "假設工程",
    "基礎工程": "土方及基礎工程",
    "土方工程": "土方及基礎工程",
    "安全觀測工程": "安全觀測工程",
    "防水工程": "防水工程",
    "門窗工程": "門窗工程",
    "金屬工程": "外部裝修工程",
    "外牆工程": "外部裝修工程",
    "屋頂工程": "防水工程",
    "裝修工程": "內外牆裝修工程",
    "水電工程": "雜項工程",
    "弱電工程": "雜項工程",
    "電氣工程": "雜項工程",
    "給排水工程": "雜項工程",
    "消防工程": "雜項工程",
    "空調工程": "雜項工程",
    "電梯工程": "雜項工程",
    "景觀工程": "景觀工程",
    "雜項工程": "雜項工程",
}

SHEET_BY_CATEGORY = {
    "結構體工程": 0, "泥作工程": 2,
    "假設工程": 1, "基礎工程": 1, "土方工程": 1, "安全觀測工程": 1,
    "防水工程": 3, "門窗工程": 3, "金屬工程": 3,
    "外牆工程": 3, "屋頂工程": 3,
    "水電工程": 4, "弱電工程": 4, "電氣工程": 4,
    "給排水工程": 4, "消防工程": 4, "空調工程": 4,
}


def read_source(source):
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("generator_version") != "2.0.0" or manifest.get("data_origin") != "SYNTHETIC":
        raise ValueError("requires SYNTHETIC v2 source")
    for filename in ("projects.csv", "historical_transactions.csv", "project_boq_rows.csv"):
        actual = hashlib.sha256((source / filename).read_bytes()).hexdigest()
        if actual != manifest["files_sha256"].get(filename):
            raise ValueError("SHA256 mismatch: " + filename)
    def read(name):
        with (source / name).open(encoding="utf-8-sig", newline="") as handle:
            return list(csv.DictReader(handle))
    projects = read("projects.csv")
    history = read("historical_transactions.csv")
    boq = read("project_boq_rows.csv")
    if len(projects) != manifest["project_count"] or len(history) != manifest["transaction_count"] or len(boq) != manifest["boq_line_count"]:
        raise ValueError("manifest record count mismatch")
    transaction = {tx["synthetic_transaction_id"]: tx for tx in history}
    if len(transaction) != len(history):
        raise ValueError("duplicate transaction IDs")
    for row in boq:
        tx = transaction.get(row["synthetic_transaction_id"])
        if not tx or any(tx[key] != row[source_key] for key, source_key in [
            ("synthetic_project_id", "synthetic_project_id"),
            ("material_code", "material_code"),
            ("actual_quantity", "quantity"),
            ("unit_price", "unit_price"),
            ("split", "split")]):
            raise ValueError("history / BOQ mismatch")
    return projects, boq


def export(source, output, truth, project_id=None, seed=147):
    if output.exists() or truth.exists() or output.resolve() == truth.resolve():
        raise ValueError("refusing to overwrite output")
    projects, boq = read_source(source)
    selected = (next((x for x in projects if x["synthetic_project_id"] == project_id), None)
                if project_id else next((x for x in projects if x["split"] == "test"), None))
    if not selected:
        raise ValueError("unknown synthetic project")
    items = [x for x in boq if x["synthetic_project_id"] == selected["synthetic_project_id"]]
    if not items:
        raise ValueError("no project BOQ lines")
    rng = random.Random(f"{seed}:{selected['synthetic_project_id']}")
    # Drop empty disciplines, preserving 1 summary and 3-6 actual detail sheets.
    categorized = defaultdict(list)
    for row in items:
        idx = SHEET_BY_CATEGORY.get(row["major_category"], 5)
        if idx == 0 and row["major_category"] == "結構體工程" and row["material_code"].startswith("SYN-WORK"):
            idx = 2
        categorized[idx].append(row)
    sheets = {"工程總表": [
        ["合成建案標單（非真實工程）"],
        ["工程名稱", selected["project_name"]],
        ["結構型式", selected["structure_type"]],
        ["樓地板面積", selected["total_floor_area"]],
        ["說明", "僅供解析與流程測試，未經施工估算驗證"],
    ]}
    truth_rows = []
    for idx in sorted(categorized):
        label = SHEETS[idx]
        headers = [*LAYOUTS[idx], "工程大項"]
        body = [[f"施工估價明細／{label}"], ["建案", selected["synthetic_project_id"]], [],
                headers, ["壹", label]]
        for n, raw in enumerate(categorized[idx], start=1):
            # A few deliberately defective rows are represented as defects
            # in the Excel only. Source history remains complete and separate.
            scenario = ("missing_quantity" if n % 47 == 0 else
                        "missing_unit_price" if n % 29 == 0 else
                        "missing_unit" if n % 37 == 0 else "complete")
            q = raw["quantity"] if scenario != "missing_quantity" else None
            price = raw["unit_price"] if scenario != "missing_unit_price" else None
            unit = raw["unit"] if scenario != "missing_unit" else None
            amount = raw["amount"] if scenario not in ("missing_quantity", "missing_unit_price") else None
            if raw["major_category"] not in APPROVED_MAJOR_CATEGORY:
                raise ValueError("Unmapped source trade: " + raw["major_category"])
            record = {
                "工程大項": APPROVED_MAJOR_CATEGORY[raw["major_category"]],
                "項次": str(n), "編號": str(n), "序號": str(n),
                "項目名稱": raw["item_name"], "工項名稱": raw["item_name"],
                "品名": raw["item_name"], "項目及說明": raw["item_name"],
                "名稱與說明": raw["item_name"],
                "規格": raw["spec"], "材料規格": raw["spec"],
                "單位": unit, "數量": q, "工程數量": q,
                "單價": price, "金額": amount, "總價": amount, "複價": amount,
                "備註": "SYNTHETIC"}
            # Numeric OOXML values should remain numeric, not text.
            for key in ("數量", "工程數量", "單價", "金額", "總價", "複價"):
                if record[key] is not None:
                    from decimal import Decimal
                    record[key] = Decimal(record[key])
            body.append([record.get(key, "") for key in headers])
            truth_rows.append({
                "sheet": label, "excel_row": len(body), "synthetic_line_id": raw["synthetic_line_id"],
                "synthetic_transaction_id": raw["synthetic_transaction_id"],
                "material_code": raw["material_code"], "category": raw["major_category"],
                "analysis_major_category": record["工程大項"],
                "rendered_item_name": raw["item_name"], "source_quantity": raw["quantity"],
                "source_unit_price": raw["unit_price"], "scenario": scenario,
                "expected_quantity": q, "expected_unit_price": price,
                "source_data_origin": "SYNTHETIC"
            })
        body.extend([[], ["備註", "測試用工程明細"], ["合計", "請勿用於正式決標"]])
        sheets[label] = body
    # Do not invent empty disciplines solely to fill a fixed seven-sheet
    # template. Layouts remain associated with sheet identity.
    sheets = {"工程總表": sheets["工程總表"],
              **{name:sheets[name] for name in SHEETS if name in sheets}}
    layouts = {name: [*LAYOUTS[idx], "工程大項"] for idx, name in enumerate(SHEETS)}
    blob = make_xlsx(sheets, headers_by_sheet=layouts)
    out = {"generator_version": "2.0.0", "data_origin": "SYNTHETIC",
           "project": selected, "xlsx_sha256": hashlib.sha256(blob).hexdigest(),
           "total_detail_rows": len(truth_rows), "detail_rows_per_sheet": dict(Counter(x["sheet"] for x in truth_rows)),
           "cases": truth_rows,
           "limitations": ["Excel defects are intentional; source history stays complete.",
                           "Synthetic history rows are NOT real transactions.",
                           "The project attributes and quantities are not calibrated to construction specifications."]}
    output.parent.mkdir(parents=True, exist_ok=True)
    truth.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(blob)
    truth.write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return {"project_id": selected["synthetic_project_id"],
            "detail_rows": len(truth_rows), "worksheets": len(sheets),
            "detail_rows_per_sheet": out["detail_rows_per_sheet"],
            "sha256": out["xlsx_sha256"]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--truth", type=Path, required=True)
    parser.add_argument("--project-id")
    args = parser.parse_args()
    try:
        print(json.dumps(export(args.source, args.output, args.truth, args.project_id), ensure_ascii=False, indent=2))
    except (ValueError, KeyError, OSError, json.JSONDecodeError) as error:
        parser.error(str(error))


if __name__ == "__main__":
    main()
