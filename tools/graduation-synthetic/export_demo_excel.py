#!/usr/bin/env python3
"""Export one synthetic project to an Excel workbook for TenderExcelParser. Offline only."""
import argparse
import csv
import hashlib
import json
import re
from decimal import Decimal, InvalidOperation
from pathlib import Path
from xml.sax.saxutils import escape
from zipfile import ZipFile, ZIP_DEFLATED, ZipInfo

BASE = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
HEADERS = ["項次", "項目名稱", "規格", "單位", "數量", "單價", "金額"]
FIXED_TIME = (2020, 1, 1, 0, 0, 0)

def fail(message):
    raise ValueError(message)

def load_rows(path):
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))

def checked_decimal(value, field):
    try:
        x = Decimal(str(value))
    except (InvalidOperation, TypeError):
        fail(f"invalid {field}")
    if not x.is_finite() or x <= 0:
        fail(f"{field} must be positive and finite")
    return x

def column(i):
    chars = ""
    while i:
        i, rem = divmod(i - 1, 26)
        chars = chr(65 + rem) + chars
    return chars

def string_cell(ref, value):
    return f'<c r="{ref}" t="inlineStr"><is><t>{escape(str(value))}</t></is></c>'

def number_cell(ref, value):
    return f'<c r="{ref}"><v>{value}</v></c>'

def sheet_xml(rows):
    data = []
    for number, record in enumerate([HEADERS, *rows], 1):
        cells = []
        for i, value in enumerate(record, 1):
            ref = f"{column(i)}{number}"
            cells.append(number_cell(ref, value) if number > 1 and i in (5, 6, 7) else string_cell(ref, value))
        data.append(f'<row r="{number}">{"".join(cells)}</row>')
    return f'<?xml version="1.0" encoding="utf-8"?><worksheet xmlns="{BASE}"><sheetData>{"".join(data)}</sheetData></worksheet>'

def xlsx_bytes(rows):
    # Deterministic, minimal Office Open XML workbook; no formulas or macros.
    import io
    output = io.BytesIO()
    parts = {
      "[Content_Types].xml": f'''<?xml version="1.0" encoding="utf-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>''',
      "_rels/.rels": f'''<?xml version="1.0" encoding="utf-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="{REL}/officeDocument" Target="xl/workbook.xml"/></Relationships>''',
      "xl/workbook.xml": f'''<?xml version="1.0" encoding="utf-8"?><workbook xmlns="{BASE}" xmlns:r="{REL}"><sheets><sheet name="材料明細" sheetId="1" r:id="rId1"/></sheets></workbook>''',
      "xl/_rels/workbook.xml.rels": f'''<?xml version="1.0" encoding="utf-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="{REL}/worksheet" Target="worksheets/sheet1.xml"/></Relationships>''',
      "xl/worksheets/sheet1.xml": sheet_xml(rows),
    }
    with ZipFile(output, "w", compression=ZIP_DEFLATED) as workbook:
        for name, payload in parts.items():
            z = ZipInfo(name, FIXED_TIME)
            z.compress_type = ZIP_DEFLATED
            workbook.writestr(z, payload.encode("utf-8"))
    return output.getvalue()

def export(source, destination, requested_id=None):
    if destination.exists():
        fail("refusing to overwrite existing Excel file")
    manifest_path = source / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("data_origin") != "SYNTHETIC":
        fail("source manifest must identify SYNTHETIC data")
    for name in ("projects.csv", "historical_transactions.csv"):
        actual = hashlib.sha256((source / name).read_bytes()).hexdigest()
        if actual != manifest.get("files_sha256", {}).get(name):
            fail(f"SHA256 mismatch: {name}")
    projects = load_rows(source / "projects.csv")
    transactions = load_rows(source / "historical_transactions.csv")
    if len(projects) != manifest.get("project_count") or len(transactions) != manifest.get("transaction_count"):
        fail("manifest row counts mismatch")
    project_ids = {p["synthetic_project_id"] for p in projects}
    if len(project_ids) != len(projects) or not all(re.fullmatch(r"SYN-[0-9]{5}", x) for x in project_ids):
        fail("invalid or duplicate synthetic project IDs")
    if any(t.get("synthetic_project_id") not in project_ids for t in transactions):
        fail("orphan synthetic transaction")
    if requested_id is None:
        project = next((p for p in projects if p["split"] == "test"), None)
    else:
        project = next((p for p in projects if p["synthetic_project_id"] == requested_id), None)
    if project is None:
        fail("requested synthetic project not found in source")
    pid = project["synthetic_project_id"]
    if project.get("data_origin") != "SYNTHETIC":
        fail("not a synthetic project")
    rows = []
    for transaction in transactions:
        if transaction["synthetic_project_id"] != pid:
            continue
        if transaction.get("data_origin") != "SYNTHETIC" or transaction["split"] != project["split"]:
            fail("transaction provenance/split mismatch")
        qty = checked_decimal(transaction["actual_quantity"], "quantity")
        price = checked_decimal(transaction["unit_price"], "unit price")
        item = transaction["material_name"]
        unit = transaction["unit"]
        if not item or not unit:
            fail("missing item name or unit")
        rows.append([str(len(rows) + 1), item, transaction["material_code"], unit,
                     str(qty), str(price), str((qty * price).quantize(Decimal(".01")))])
    if not rows:
        fail("no transactions for selected project")
    result = xlsx_bytes(rows)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(result)
    return {"project_id": pid, "project_split": project["split"], "item_count": len(rows),
            "xlsx_sha256": hashlib.sha256(result).hexdigest(),
            "file": str(destination), "project_details": {
                k: project[k] for k in ("project_type", "structure_type", "city",
                    "total_floor_area", "floor_count", "basement_floor_count", "start_date")}}

def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--project-id", default=None)
    args = parser.parse_args(argv)
    try:
        print(json.dumps(export(args.source, args.output, args.project_id), ensure_ascii=False, indent=2))
    except (ValueError, OSError, KeyError, json.JSONDecodeError) as error:
        parser.error(str(error))

if __name__ == "__main__":
    main()
