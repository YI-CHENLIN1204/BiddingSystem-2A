"""Map privately authorized final-award spreadsheets to intake CSV files."""
import argparse
import csv
from datetime import date
from decimal import Decimal, InvalidOperation
import hashlib
import json
from pathlib import Path

from intake import CASES, LINES, digest, index, read, require, write

FIELDS={"line_id","item","spec","unit","quantity","unit_price","amount","price_state"}
def sheets(path, selectors):
    if path.suffix.lower()==".csv":
        require(selectors==["csv"],"CSV requires sheets ['csv']")
        with path.open(encoding="utf-8-sig",newline="") as stream:
            yield "csv", list(csv.reader(stream))
    else:
        require(path.suffix.lower()==".xlsx","Use .xlsx or UTF-8 CSV")
        from openpyxl import load_workbook
        book=load_workbook(path,read_only=True,data_only=False,keep_links=False)
        try:
            for name in selectors:
                require(name in book.sheetnames,"Selected sheet missing")
                yield name, list(book[name].values)
        finally:
            book.close()

def decimal(value, field):
    try:
        result=Decimal(str(value))
        require(result.is_finite() and result>0,f"Invalid {field}")
        return result
    except InvalidOperation:
        raise ValueError(f"Invalid {field}") from None

def prepare(source, config_path, output):
    config=json.loads(config_path.read_text(encoding="utf-8-sig"))
    cases=read(source/"cases.csv",CASES)
    keys=index(cases,("case_id",))
    entries=config["awards"]
    require(entries and not output.exists(),"Empty awards or existing destination")
    require(set(index(entries,("case_id",)))==set(keys),"Exactly one final version per case is required")
    columns=config["columns"]
    require(set(columns)==FIELDS and len(set(columns.values()))==len(FIELDS),"Invalid column mapping")
    units=config["units"]
    seen_hash, seen_content, result, private = set(),set(),[],[]
    for entry in entries:
        key=entry["case_id"]
        file=(source/entry["path"]).resolve()
        require(file.is_relative_to(source.resolve()),"Source path outside private directory")
        expected=entry["sha256"].lower()
        require(expected==digest(file) and expected not in seen_hash,"File SHA mismatch or repeated workbook")
        seen_hash.add(expected)
        require(entry["authorization_ref"].strip() and entry["version"].strip(),"Approval and version required")
        require(entry["first_available_date"]==keys[(key,)]["available_date"],"Availability metadata mismatch")
        date.fromisoformat(entry["first_available_date"])
        selectors=entry["sheets"]
        require(selectors and len(set(selectors))==len(selectors),"Select unique detail sheets")
        batch=[]
        for name, rows in sheets(file,selectors):
            header_row=entry.get("header_row",1)
            require(isinstance(header_row,int) and 1<=header_row<=len(rows),"Invalid header row")
            header=[str(x).strip() if x is not None else "" for x in rows[header_row-1]]
            require(all(header.count(v)==1 for v in columns.values()),"Missing or ambiguous column header")
            offsets={k:header.index(v) for k,v in columns.items()}
            for ordinal,row in enumerate(rows[header_row:],header_row+1):
                if not any(x is not None and str(x).strip() for x in row):
                    continue
                values={k:str(row[i]).strip() if i<len(row) and row[i] is not None else "" for k,i in offsets.items()}
                try:
                    require(not any(v.startswith("=") for v in values.values()),"Expected literal values")
                    require(values["price_state"]==config["final_price_state"],"Pending/non-final price")
                    require(values["line_id"],"Missing source row ID")
                    qty=decimal(values["quantity"],"quantity")
                    price=decimal(values["unit_price"],"unit price")
                    total=decimal(values["amount"],"extended amount")
                    require(abs(total-qty*price)<=Decimal("0.01"),"Extended amount mismatch")
                    require(values["unit"] in units,"Unapproved unit")
                    codes=config["item_specs"].get(values["item"],{}).get(values["spec"])
                    require(codes and units[values["unit"]]==codes["unit_code"],"Unapproved item/spec/unit combination")
                    batch.append(dict(case_id=key,line_id=values["line_id"],item_code=codes["item_code"],
                                      spec_code=codes["spec_code"],unit_code=codes["unit_code"],
                                      quantity=str(qty.normalize()),unit_price=str(price.normalize()),currency=entry["currency"]))
                    private.append(dict(case_id=key,line_id=values["line_id"],sheet=name,row=ordinal,
                                        sha256=expected,version=entry["version"],approval_ref=entry["authorization_ref"]))
                except ValueError as exc:
                    raise ValueError(f"{name}: row {ordinal}: {exc}") from None
        require(digest(file)==expected,"Source changed during import")
        require(batch,"No importable final lines")
        index(batch,("case_id","line_id"))
        canonical=sorted(tuple(row[k] for k in LINES[2:]) for row in batch)
        fp=hashlib.sha256(json.dumps(canonical).encode()).hexdigest()
        require(fp not in seen_content,"Duplicate normalized workbook content")
        seen_content.add(fp)
        result.extend(batch)
    output.mkdir()
    write(output/"cases.csv",CASES,cases)
    write(output/"lines.csv",LINES,result)
    write(output/"PRIVATE_line_provenance.csv",tuple(private[0]),private)
    (output/"import-receipt.json").write_text(json.dumps(dict(case_count=len(cases),line_count=len(result),
        map_sha256=digest(config_path),outputs={n:digest(output/n) for n in ("cases.csv","lines.csv")},
        status="validated_mapping_pending_split_and_freeze"),indent=2)+"\n",encoding="utf-8")

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    for field in ("source","mapping","output"):
        parser.add_argument(field,type=Path)
    a=parser.parse_args()
    prepare(a.source,a.mapping,a.output)
