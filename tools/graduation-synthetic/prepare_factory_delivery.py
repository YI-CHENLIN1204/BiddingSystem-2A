#!/usr/bin/env python3
"""Prepare a portable factory delivery and TRAIN-ONLY import candidates. Offline."""
import argparse
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED
from export_full_project_excel import export

def read_csv(path):
    with path.open(encoding="utf-8-sig",newline="") as f:
        reader=csv.DictReader(f);return list(reader),reader.fieldnames

def write_csv(path,rows,fields):
    with path.open("w",encoding="utf-8-sig",newline="") as f:
        w=csv.DictWriter(f,fieldnames=fields,lineterminator="\n");w.writeheader();w.writerows(rows)

def prepare(source,output):
    source,output=Path(source),Path(output)
    if output.exists() or (source/"delivery").exists():
        raise ValueError("refusing to overwrite an existing archive or delivery directory")
    manifest=json.loads((source/"manifest.json").read_text(encoding="utf-8"))
    if manifest.get("generator_version")!="factory-1.0.0" or manifest.get("data_origin")!="SYNTHETIC":
        raise ValueError("requires the explicit synthetic factory batch")
    for name,expected in manifest["files_sha256"].items():
        if Path(name).name!=name or hashlib.sha256((source/name).read_bytes()).hexdigest()!=expected:
            raise ValueError("source checksum/name mismatch: "+name)
    projects,pfields=read_csv(source/"projects.csv")
    boq,bfields=read_csv(source/"project_boq_rows.csv")
    history,hfields=read_csv(source/"historical_transactions.csv")
    train=[p for p in projects if p["split"]=="train"]
    ids={p["synthetic_project_id"] for p in train}
    assert len(ids)==126 and all(p["project_type"]=="廠房" for p in train)
    train_boq=[r for r in boq if r["synthetic_project_id"] in ids]
    train_history=[r for r in history if r["synthetic_project_id"] in ids]
    assert all(r["split"]=="train" for r in train_boq+train_history)
    assert len(train_boq)==len(train_history)
    delivery=source/"delivery";candidate=delivery/"import_candidate";candidate.mkdir(parents=True)
    for name,rows,fields in [("train_projects.csv",train,pfields),("train_project_boq_rows.csv",train_boq,bfields),
                            ("train_historical_transactions.csv",train_history,hfields)]:
        write_csv(candidate/name,rows,fields)
    used={r["material_code"] for r in train_boq}
    catalog,_=read_csv(source/"material_catalog.csv")
    required=[m for m in catalog if m["material_code"] in used]
    preflight={"status":"NOT_IMPORTED","automatic_approval_allowed":False,
        "target":"separately confirmed synthetic test database only",
        "project_count":len(train),"boq_rows":len(train_boq),"history_rows":len(train_history),
        "by_structure":dict(Counter(p["structure_type"] for p in train)),
        "excluded_splits":["validation","test","paired_diagnostics"],
        "required_materials":required,
        "mapping_rule":"match exact material name + unit; database ID hints are not portable; review the four new factory templates",
        "scope_rule":"illustrative selected trades; do not claim a complete real structural contract",
        "writes_performed":False,"residential_replacement_allowed":False}
    (candidate/"preflight.json").write_text(json.dumps(preflight,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    workbook_results=[]
    for split in ("train","test"):
        for structure in ("RC","SC","SRC"):
            p=next(p for p in projects if p["split"]==split and p["structure_type"]==structure and
                p["factory_scenario"]=="basement_production")
            workbook=delivery/"workbooks"/split/f"factory-{structure}-complete.xlsx"
            truth=workbook.with_suffix(".truth.json")
            result=export(source,workbook,truth,p["synthetic_project_id"],include_defects=False)
            workbook_results.append({"split":split,"structure":structure,**result})
    readme="""# 廠房合成資料 v1（2026-11-22）

180案：RC/SC/SRC各60；126 train / 27 validation / 27 test。
全部是明示假設的模擬資料，不是真實標單、工程標準、供應商報價或已驗證的異常真值。
不會修改舊住宅資料；此包沒有執行資料庫匯入或核准。

## 內容
- projects.csv / project_boq_rows.csv / historical_transactions.csv：完整基準資料及一對一來源。
- project_material_applicability.csv：ApplicablePresent、NotApplicableAssumed、OutOfScope；缺項不是零。
- assumptions.json：所有主要生成關係和限制；不要把假設係數當作工程規範。
- diagnostic_*.csv：三個既有test案的六個配對缺項/零量變體，不是六個獨立建案，不得匯入歷史。
- delivery/workbooks/train：三種結構的完整單價範例，可作隔離匯入流程測試。
- delivery/workbooks/test：三個獨立test案範例；不可發布為Approved參考，避免汙染Holdout。
- delivery/import_candidate：只含126個train案及配對列，preflight.json列待對應材料與限制。
- delivery-manifest.json：封裝檔案雜湊與範例清單。

## 缺項原則
NotApplicableAssumed：此合成情境明確未配置，並非真實工程不適用認證。
OutOfScope：本批沒有模擬；不知道是否施作，不能自動補零或當作不適用。
MissingRequiredItem：測試用刻意漏列；不等於工程異常。
ExplicitRecordedZero：測試來源明確填零；與沒有工項不同。

## 匯入前
先確認獨立合成測試目標、保留住宅備份、對照名稱與單位及四種新材料。
此包不是自動執行的SQL，也不包含資料庫連線或密碼。
不能把validation/test/diagnostic放進歷史；不能自動核准或取代既有資料。
鋼骨、模板、土方估算需先啟動含19a4c508及後續修正的新版前後端。

## 可重現程式
GitHub YI-CHENLIN1204/BiddingSystem-2A，feature/1d-api-integration：
tools/graduation-synthetic/generate_factory_dataset.py
tools/graduation-synthetic/prepare_factory_delivery.py
種子20261122；manifest.json保留生成器與來源雜湊。
"""
    (delivery/"README.md").write_text(readme,encoding="utf-8")
    names=[source/name for name in manifest["files_sha256"]]+[source/"manifest.json"]
    names+=sorted(delivery.rglob("*"))
    names=[p for p in names if p.is_file()]
    files={p.relative_to(source).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(names)}
    package={"data_origin":"SYNTHETIC","database_imported":False,"residential_modified":False,
        "files_sha256":files,"workbooks":workbook_results,"train_projects":len(train),
        "train_boq_rows":len(train_boq),"new_catalog_templates":4}
    index=source/"delivery-manifest.json"
    index.write_text(json.dumps(package,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    names.append(index)
    output.parent.mkdir(parents=True,exist_ok=True)
    with ZipFile(output,"x",compression=ZIP_DEFLATED) as archive:
        for p in sorted(names):
            info=ZipInfo(p.relative_to(source).as_posix(),date_time=(2020,1,1,0,0,0))
            info.compress_type=ZIP_DEFLATED
            archive.writestr(info,p.read_bytes())
    return {**{k:package[k] for k in ["train_projects","train_boq_rows","new_catalog_templates"]},
            "files":len(names),"workbooks":workbook_results,
            "archive_sha256":hashlib.sha256(output.read_bytes()).hexdigest(),"archive_bytes":output.stat().st_size}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--source",type=Path,required=True);p.add_argument("--output",type=Path,required=True)
    args=p.parse_args()
    try:print(json.dumps(prepare(args.source,args.output),ensure_ascii=False))
    except (ValueError,AssertionError,OSError) as e:p.error(str(e))

if __name__=="__main__":main()
