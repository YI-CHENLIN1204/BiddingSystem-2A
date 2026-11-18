#!/usr/bin/env python3
"""Produce reproducible illustrative graduation data. No DB writes."""
import argparse
import csv
import hashlib
import json
import random
from datetime import date, timedelta
from pathlib import Path

SCENARIOS = {
    "reinforced_concrete": {"structure": "RC", "materials": [("SYN-REBAR", "示意鋼筋", "kg", 80.0, 0.18), ("SYN-CONCRETE", "示意混凝土", "m3", 0.42, 0.14)]},
    "steel": {"structure": "SC", "materials": [("SYN-STEEL", "示意結構鋼", "kg", 65.0, 0.18), ("SYN-CONCRETE", "示意混凝土", "m3", 0.26, 0.16)]},
}
PROJECT_FIELDS = ["synthetic_project_id","project_name","project_type","structure_type","city","total_floor_area","floor_count","basement_floor_count","start_date","data_origin","split"]
HISTORY_FIELDS = ["synthetic_transaction_id","synthetic_project_id","material_code","material_name","unit","actual_quantity","unit_price","transaction_date","data_origin","synthetic_label","split"]

def write_csv(path, fields, rows):
    with path.open("w", newline="", encoding="utf-8-sig") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)

def generate(count, seed):
    rng = random.Random(seed)
    projects, histories = [], []
    ids = list(range(1, count + 1))
    rng.shuffle(ids)
    a, b = int(count * .70), int(count * .85)
    split_by_id = {n: ("train" if i < a else "validation" if i < b else "test") for i,n in enumerate(ids)}
    for i in range(1, count + 1):
        kind = rng.choice(tuple(SCENARIOS))
        base = SCENARIOS[kind]
        floor_count = rng.randint(4, 25)
        basement = rng.randint(0, 4)
        area = round(floor_count * rng.uniform(330, 1250) + basement * rng.uniform(350, 1400), 2)
        start = date(2016, 1, 1) + timedelta(days=rng.randint(0, 3300))
        pid = f"SYN-{i:05d}"
        split = split_by_id[i]
        projects.append({"synthetic_project_id":pid,"project_name":f"合成住宅建案-{i:05d}",
                         "project_type":"住宅","structure_type":base["structure"],"city":rng.choice(["臺中市","臺南市","高雄市","桃園市"]),
                         "total_floor_area":f"{area:.2f}","floor_count":floor_count,"basement_floor_count":basement,
                         "start_date":start.isoformat(),"data_origin":"SYNTHETIC","split":split})
        for j,(code,name,unit,coefficient,variation) in enumerate(base["materials"],1):
            multiplier = rng.uniform(1-variation,1+variation)
            quantity = round(max(.01, area * coefficient * multiplier), 3)
            anomaly = rng.random() < .08
            if anomaly: quantity = round(quantity * rng.choice([.35,1.95]), 3)
            unit_price = round(rng.uniform(10,100), 2)
            histories.append({"synthetic_transaction_id":f"SYNTX-{i:05d}-{j}",
                              "synthetic_project_id":pid,"material_code":code,"material_name":name,"unit":unit,
                              "actual_quantity":f"{quantity:.3f}","unit_price":f"{unit_price:.2f}",
                              "transaction_date":(start+timedelta(days=60)).isoformat(),
                              "data_origin":"SYNTHETIC","synthetic_label":"injected_anomaly" if anomaly else "baseline",
                              "split":split})
    return projects,histories

def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--count",type=int,default=300)
    parser.add_argument("--seed",type=int,default=20261119)
    parser.add_argument("--output",type=Path,required=True)
    args = parser.parse_args(argv)
    if not (20 <= args.count <= 10000): parser.error("--count must be between 20 and 10000")
    if args.output.exists() and any(args.output.iterdir()): parser.error("output directory must be empty to avoid accidental overwrite")
    args.output.mkdir(parents=True,exist_ok=True)
    projects,histories = generate(args.count,args.seed)
    for name,fields,rows in [("projects.csv",PROJECT_FIELDS,projects),("historical_transactions.csv",HISTORY_FIELDS,histories)]:
        write_csv(args.output/name,fields,rows)
    hashes={name:hashlib.sha256((args.output/name).read_bytes()).hexdigest() for name in ["projects.csv","historical_transactions.csv"]}
    manifest={"generator_version":"0.1.0","data_origin":"SYNTHETIC","seed":args.seed,"project_count":len(projects),
              "transaction_count":len(histories),"files_sha256":hashes,
              "limitations":["Illustrative quantities and prices only; not validated engineering standards or enterprise data.",
                             "No database writes; cannot establish real-world estimation accuracy.",
                             "Material codes are synthetic and must be mapped in isolated demo environment."]}
    (args.output/"manifest.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"projects":len(projects),"transactions":len(histories),"output":str(args.output),"hashes":hashes},ensure_ascii=False))

if __name__=="__main__": main()
