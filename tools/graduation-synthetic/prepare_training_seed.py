#!/usr/bin/env python3
"""Prepare strictly TRAIN-only synthetic rows for a *separate* graduation DB.

This never connects to a database. ASP.NET/EF Core importer must validate its
destination and explicitly authorize a disposable localhost database.
"""
import argparse
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path


def prepare(source):
    manifest=json.loads((source/"manifest.json").read_text(encoding="utf-8"))
    if manifest.get("generator_version")!="2.0.0" or manifest.get("data_origin")!="SYNTHETIC":
        raise ValueError("SYNTHETIC v2 manifest required")
    def read(filename):
        if hashlib.sha256((source/filename).read_bytes()).hexdigest()!=manifest["files_sha256"].get(filename):
            raise ValueError("SHA256 mismatch: "+filename)
        with (source/filename).open(encoding="utf-8-sig", newline="") as f:
            return list(csv.DictReader(f))
    projects=read("projects.csv")
    txs=read("historical_transactions.csv")
    boq=read("project_boq_rows.csv")
    if len(projects)!=manifest["project_count"] or len(txs)!=manifest["transaction_count"] or len(boq)!=manifest["boq_line_count"]:
        raise ValueError("source counts mismatch")
    index={p["synthetic_project_id"]:p for p in projects}
    if len(index)!=len(projects):
        raise ValueError("duplicate projects")
    by_id={r["synthetic_transaction_id"]:r for r in boq}
    if len(by_id)!=len(boq) or len({t["synthetic_transaction_id"] for t in txs})!=len(txs):
        raise ValueError("duplicate transactions")
    for tx in txs:
        row=by_id.get(tx["synthetic_transaction_id"])
        project=index.get(tx["synthetic_project_id"])
        if row is None or project is None or any(row[a]!=tx[b] for a,b in [
            ("synthetic_project_id","synthetic_project_id"),
            ("material_code","material_code"),
            ("quantity","actual_quantity"),("unit_price","unit_price"),("split","split")
        ]) or project["split"]!=tx["split"]:
            raise ValueError("BOQ/history/metadata lineage failure")
    train_ids={p["synthetic_project_id"] for p in projects if p["split"]=="train"}
    if not train_ids:
        raise ValueError("no train projects")
    train_tx=[t for t in txs if t["synthetic_project_id"] in train_ids]
    train_projects=[p for p in projects if p["synthetic_project_id"] in train_ids]
    source_item_by_tx={b["synthetic_transaction_id"]:b for b in boq}
    materials={}
    for t in train_tx:
        code=t["material_code"]
        detail=source_item_by_tx[t["synthetic_transaction_id"]]
        item={"code":code,"unit":t["unit"],"category":detail["major_category"],
              "name":("鋼筋" if code=="SYN-REBAR" else
                      "混凝土" if code=="SYN-CONCRETE" else
                      "結構鋼骨" if code=="SYN-STEEL" else
                      detail["item_name"].split("－")[0])}
        if code in materials and (materials[code]["unit"],materials[code]["category"])!=(item["unit"],item["category"]):
            raise ValueError("material code changed unit or category: "+code)
        materials[code]=item
    cleaned=[]
    for t in train_tx:
        cleaned.append({"synthetic_transaction_id":t["synthetic_transaction_id"],
                        "synthetic_project_id":t["synthetic_project_id"],
                        "material_code":t["material_code"],
                        "quantity":t["actual_quantity"],
                        "transaction_date":t["transaction_date"],
                        "is_outlier":t["synthetic_label"]=="injected_anomaly"})
    return {"data_origin":"SYNTHETIC","dataset_version":"2.0.0",
            "target_database":"bidding2a_graduation",
            "split":"train","source_manifest_sha256":hashlib.sha256((source/"manifest.json").read_bytes()).hexdigest(),
            "train_project_count":len(train_projects),"train_transaction_count":len(cleaned),
            "excluded_holdout_projects":len(projects)-len(train_projects),
            "project_rows":train_projects,
            "material_rows":sorted(materials.values(),key=lambda x:x["code"]),
            "transaction_rows":cleaned,
            "disclaimer":"Disposable localhost graduation database ONLY. Do not upload to collaboration production database."}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    if args.output.exists():parser.error("refusing overwrite")
    try:
        result=prepare(args.source)
    except (ValueError,KeyError,OSError,json.JSONDecodeError) as e:
        parser.error(str(e))
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({k:v for k,v in result.items() if k not in
                      ("project_rows","material_rows","transaction_rows")},ensure_ascii=False,indent=2))


if __name__=="__main__":
    main()
