#!/usr/bin/env python3
"""Offline hypothetical factory BOQs. No DB/client/network/write-to-history code.

Quantities and prices are simulation assumptions, not engineering norms,
supplier quotes or real final contracts. Applicability truth is separate from
model inputs; absence is never silently converted to quantity zero.
"""
import argparse
import csv
import hashlib
import json
import math
import random
from collections import Counter, defaultdict
from datetime import date
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

VERSION = "factory-1.0.0"
SCHEMA = "2.0.0"
STRUCTURES = ("RC", "SC", "SRC")
SCENARIOS = ("single_storey_workshop", "warehouse_with_office", "multistorey_production",
             "basement_production", "coated_floor_workshop", "loading_yard_facility")
SPLITS = ("train",)*42 + ("validation",)*9 + ("test",)*9
PROJECT_FIELDS = ["synthetic_project_id","project_name","project_type","structure_type","city",
    "total_floor_area","site_area","floor_count","basement_floor_count","start_date","data_origin","split",
    "factory_scenario","footprint_area","aboveground_floor_area","basement_floor_area","office_area",
    "roof_system","wall_system","foundation_system","floor_system","span_m","load_class"]
BOQ_FIELDS = ["synthetic_line_id","synthetic_transaction_id","synthetic_project_id","material_code",
    "major_category","item_name","spec","unit","quantity","unit_price","amount","work_zone","floor_label",
    "structure_type","data_origin","split","price_scope"]
HISTORY_FIELDS = ["synthetic_transaction_id","synthetic_project_id","material_code","material_name",
    "unit","actual_quantity","unit_price","transaction_date","data_origin","synthetic_label","split"]
TRUTH_FIELDS = ["synthetic_project_id","material_code","status","reason","expected_quantity","unit","split"]
CATALOG_PATH = Path(__file__).with_name("factory_material_catalog.json")
# Hypothetical construction relationships, not measured/calibrated rates.
ASSUMPTIONS = {
    "classification":"SYNTHETIC_FACTORY_WORKFLOW_ONLY", "engineering_calibrated":False,
    "geometry":"floor area = footprint * floors + footprint * basement_count * 0.85",
    "excavation":"footprint * (basement_count * 3.6 + assumed_foundation_depth) * 1.08",
    "concrete":"footprint*0.18 + aboveground_area*structure_slab_factor + basement_area*0.24",
    "slab_factors":{"RC":"0.26","SC":"0.12","SRC":"0.22"},
    "rebar_kg_per_synthetic_concrete_m3":{"RC":"95","SC":"70","SRC":"110"},
    "steel_base_kg_per_aboveground_m2":{"RC":"0","SC":"36","SRC":"45"},
    "steel_design_effect":"base * (1 + span/100) * (1.15 if heavy_load else 1) * lognormal_noise",
    "formwork":"SC: footprint*0.8 + basement_area*1.4; RC/SRC: floor_area*2.4",
    "noise_sigma":"0.10 for structural quantities; not fitted to any real dataset",
    "price_note":"fixed hypothetical unit-price seeds, +/-8% scenario variation, toy year factor 2%/year; NOT market data",
    "scope_note":"Illustrative selected trades; not a complete structural contract, no production process machinery/civil certification",
    "not_applicable_note":"Only scenario-defined exclusions are NotApplicableAssumed; unmodelled catalog items are OutOfScope",
}

def quant(value, digits="0.001"):
    return Decimal(str(value)).quantize(Decimal(digits), rounding=ROUND_HALF_UP)

def code_for_id(mid):
    return {1:"SYN-CONCRETE",2:"SYN-REBAR",3:"SYN-STEEL"}.get(mid, f"SYN-WORK-{mid-3:03d}")

def geometry(seed, structure, index):
    project_id = f"FAC-20261122-{structure}-{index+1:04d}"
    rng = random.Random(f"{seed}/{project_id}/geometry")
    scenario = SCENARIOS[index % len(SCENARIOS)]
    floors = (1,2,rng.randint(3,6),rng.randint(2,4),2,1)[index % 6]
    if structure == "SC" and scenario == "multistorey_production": floors = rng.randint(2,4)
    basement = 1 if scenario == "multistorey_production" else 2 if scenario == "basement_production" else 0
    width, length = rng.uniform(24,72), rng.uniform(35,110)
    footprint = quant(width*length, "0.01")
    above = quant(footprint*floors, "0.01")
    below = quant(footprint*basement*Decimal("0.85"), "0.01")
    office_fraction = (Decimal("0.18") if scenario == "warehouse_with_office" else Decimal("0.06")
        if scenario in ("multistorey_production","basement_production") else Decimal(0))
    office = quant(above*office_fraction, "0.01")
    metal = structure == "SC" or scenario == "loading_yard_facility" and structure == "SRC"
    project = {
        "synthetic_project_id":project_id, "project_name":f"SYNTHETIC_FACTORY:{structure} 合成廠房-{index+1:04d}",
        "project_type":"廠房", "structure_type":structure,
        "city":rng.choice(["臺中市","桃園市","新竹縣","彰化縣","臺南市","高雄市"]),
        "total_floor_area":str(above+below), "site_area":str(quant(footprint/quant(rng.uniform(.45,.75),".0001"),".01")),
        "floor_count":floors,"basement_floor_count":basement,
        "start_date":date(rng.randint(2018,2025),rng.randint(1,12),1).isoformat(),
        "data_origin":"SYNTHETIC", "split":SPLITS[index],
        "factory_scenario":scenario,"footprint_area":str(footprint),
        "aboveground_floor_area":str(above),"basement_floor_area":str(below),"office_area":str(office),
        "roof_system":"metal_panel" if metal else "concrete_waterproof",
        "wall_system":"metal_panel" if metal else "masonry_finish",
        "foundation_system":"basement_box" if basement else "isolated_footing",
        "floor_system":"hardener" if scenario in ("coated_floor_workshop","loading_yard_facility") else "mortar_finish",
        "span_m":str(quant(width/rng.randint(2,4),".01")),
        "load_class":"heavy" if scenario=="coated_floor_workshop" else "general"
    }
    return project

def configured_quantities(project, seed):
    """A single scenario determines which work exists, then quantities follow."""
    p = project
    rng = random.Random(f"{seed}/{p['synthetic_project_id']}/quantity")
    area, footprint, above, below, office, site = (Decimal(p[k]) for k in
        ["total_floor_area","footprint_area","aboveground_floor_area","basement_floor_area","office_area","site_area"])
    basement, floors, structure = int(p["basement_floor_count"]), int(p["floor_count"]), p["structure_type"]
    noise = lambda: Decimal(str(rng.lognormvariate(0,.10)))
    concrete = (footprint*Decimal(".18")+above*Decimal(ASSUMPTIONS["slab_factors"][structure])+below*Decimal(".24"))*noise()
    rebar = concrete*Decimal(ASSUMPTIONS["rebar_kg_per_synthetic_concrete_m3"][structure])*noise()
    steel = above*Decimal(ASSUMPTIONS["steel_base_kg_per_aboveground_m2"][structure])*(1+Decimal(p["span_m"])/100)*noise()
    if p["load_class"]=="heavy": steel *= Decimal("1.15")
    template = footprint*Decimal(".8")+below*Decimal("1.4") if structure=="SC" else area*Decimal("2.4")
    foundation_depth = Decimal(".8") if not basement else Decimal("1.1")
    excavation = footprint*(Decimal(basement)*Decimal("3.6")+foundation_depth)*Decimal("1.08")
    perimeter = quant(4*site.sqrt())
    # Exactly one excavation, backfill and formwork variant per project: no
    # double-counting alternative standard work descriptions.
    earth_id = 5 if basement else 49
    backfill_id = 50 if basement else 7
    template_id = 52 if structure=="SC" else 8
    quantities = {
        1:concrete,2:rebar,earth_id:excavation,backfill_id:footprint*Decimal(".16"),
        template_id:template,4:perimeter,14:area*Decimal(".12"),16:area*Decimal(".04"),
        18:Decimal(max(2,floors*2)),19:perimeter,24:Decimal(max(1,floors)),
        25:area*Decimal(".03"),32:area*Decimal(".18"),36:Decimal(max(5,floors*8)),
        39:Decimal(max(2,floors*2)),43:Decimal(max(1,floors)),45:Decimal(4),46:Decimal(1),
        47:perimeter*Decimal("1.5"),73:Decimal(max(2,floors*2)),74:Decimal(max(2,floors*3)),
        75:perimeter*Decimal(".8"),78:Decimal(max(1,floors)),79:area*Decimal(".07"),
        80:Decimal(max(4,floors*4)),82:Decimal(1),83:area/Decimal(80),84:Decimal(max(2,floors*2)),
        85:area/Decimal(16),86:Decimal(max(1,floors)),90:max(office,footprint*Decimal(".02"))*Decimal(".15"),
        91:Decimal(max(2,floors*2)),92:Decimal(max(4,floors*3)),97:perimeter*Decimal(".5")
    }
    if structure != "RC": quantities[3] = steel
    if basement:
        quantities.update({6:Decimal(1),11:perimeter*basement,20:Decimal(12),22:perimeter,
            23:perimeter*basement,48:Decimal(2),51:perimeter*basement})
    if office > 0:
        quantities.update({12:Decimal(max(3,floors*3)),15:Decimal(max(1,floors)),
            29:office*Decimal(".3"),42:office/Decimal(12),67:office*Decimal("2"),
            87:office*Decimal(".2"),88:Decimal(max(1,floors)),89:Decimal(max(1,floors))})
    if floors > 1:
        quantities.update({13:perimeter*Decimal(".2"),26:Decimal(40*floors),69:Decimal(15*floors),81:Decimal(4*floors)})
    if p["roof_system"]=="metal_panel":
        quantities["FAC-METAL-ROOF"] = footprint*Decimal("1.08")
    else:
        quantities[27] = footprint; quantities[61] = perimeter
    if p["wall_system"]=="metal_panel":
        quantities["FAC-METAL-WALL"] = perimeter*Decimal(floors*4)*Decimal(".70")
    else:
        quantities[54] = area*Decimal(".15"); quantities[55] = perimeter*Decimal(floors*4)*Decimal(".7")
    if p["floor_system"]=="hardener":
        quantities["FAC-HARDENER-FLOOR"] = above-office
    else: quantities[56] = above-office
    if p["factory_scenario"] in ("single_storey_workshop","warehouse_with_office","loading_yard_facility"):
        quantities["FAC-ROLLING-DOOR"] = Decimal(2)
    if p["factory_scenario"]=="loading_yard_facility": quantities[98] = site-footprint
    return {code_for_id(k) if isinstance(k,int) else k:quant(v) for k,v in quantities.items()}

# The modelled universe is explicit; the other catalog materials are OutOfScope,
# not assertions that an actual factory never needs them.
MODELLED_IDS = {1,2,3,4,5,6,7,8,11,12,13,14,15,16,18,19,20,22,23,24,25,26,27,29,32,36,39,
    42,43,45,46,47,48,49,50,51,52,54,55,56,61,67,69,73,74,75,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,97,98}
MODELLED_CODES = {code_for_id(i) for i in MODELLED_IDS} | {"FAC-METAL-ROOF","FAC-METAL-WALL","FAC-ROLLING-DOOR","FAC-HARDENER-FLOOR"}
COUNT_UNITS = {"樘","組","式","座","處","點","只"}
BASE_PRICES = {"SYN-CONCRETE":Decimal(2500),"SYN-REBAR":Decimal(29),"SYN-STEEL":Decimal(52),
    "FAC-METAL-ROOF":Decimal(1250),"FAC-METAL-WALL":Decimal(1100),"FAC-ROLLING-DOOR":Decimal(65000),
    "FAC-HARDENER-FLOOR":Decimal(800)}

def unit_price(material, project, seed):
    rng = random.Random(f"{seed}/{project['synthetic_project_id']}/{material['material_code']}/price")
    base = BASE_PRICES.get(material["material_code"], {"m":Decimal(250),"m2":Decimal(650),
        "m3":Decimal(450),"kg":Decimal(35),"式":Decimal(50000),"組":Decimal(12000),
        "樘":Decimal(18000),"只":Decimal(900),"處":Decimal(400),"點":Decimal(700),"座":Decimal(20000)}[material["unit"]])
    year = int(project["start_date"][:4])
    return quant(base*(Decimal(1)+Decimal(".02")*(year-2020))*Decimal(str(rng.uniform(.92,1.08))),".01")

def split_quantity(total, unit):
    if unit in COUNT_UNITS:
        total = Decimal(math.ceil(total))
        n = min(3,int(total))
        values = [Decimal(int(total)//n)]*n
        for i in range(int(total)%n): values[i]+=1
    else:
        values=[quant(total*Decimal(".5")),quant(total*Decimal(".3"))]
        values.append(total-sum(values))
    assert all(x > 0 for x in values)
    return total, values

def produce(seed=20261122):
    catalog = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    by_code = {m["material_code"]:m for m in catalog}
    projects,boq,history,truth = [],[],[],[]
    sequence = 0
    for structure in STRUCTURES:
        for index in range(60):
            p = geometry(seed,structure,index); projects.append(p)
            quantities = configured_quantities(p,seed)
            for code in sorted(by_code):
                m = by_code[code]
                if code not in quantities:
                    status = "NotApplicableAssumed" if code in MODELLED_CODES else "OutOfScope"
                    reason = ("本合成情境未配置此工項，非真實工程不適用認證" if status=="NotApplicableAssumed" else
                              "此批未模擬的工項，不能解釋為未施作或數量零")
                    truth.append(dict(zip(TRUTH_FIELDS,[p["synthetic_project_id"],code,status,reason,"",m["unit"],p["split"]])))
                    continue
                total,parts = split_quantity(quantities[code],m["unit"])
                truth.append(dict(zip(TRUTH_FIELDS,[p["synthetic_project_id"],code,"ApplicablePresent",
                    "依已宣告廠房情境產生之假設用量",str(total),m["unit"],p["split"]])))
                price = unit_price(m,p,seed)
                for part,quantity in enumerate(parts,1):
                    sequence+=1
                    line_id=f"FACBOQ-{p['synthetic_project_id']}-{sequence:07d}"
                    tx_id=f"FACTX-{p['synthetic_project_id']}-{sequence:07d}"
                    scope = "MaterialOnly" if code in ("SYN-CONCRETE","SYN-REBAR","SYN-STEEL") else "SupplyAndInstall"
                    row = dict(zip(BOQ_FIELDS,[line_id,tx_id,p["synthetic_project_id"],code,
                        "鋼骨鋼構工程" if code=="SYN-STEEL" else m["major_category"],
                        f"{m['material_name']}－廠房作業區{part}",f"合成情境/{p['factory_scenario']}/{scope}",
                        m["unit"],str(quantity),str(price),str(quant(quantity*price,".01")),f"作業區{part}",
                        "依情境範圍",structure,"SYNTHETIC",p["split"],scope]))
                    boq.append(row)
                    history.append(dict(zip(HISTORY_FIELDS,[tx_id,p["synthetic_project_id"],code,row["item_name"],
                        m["unit"],str(quantity),str(price),p["start_date"],"SYNTHETIC","baseline",p["split"]])))
    return {"projects":projects,"boq":boq,"history":history,"applicability":truth,"catalog":catalog}

def diagnostics(dataset):
    """Six paired parser/data-quality fixtures. Never included in baseline/history."""
    project_rows,rows,truth=[],[],[]
    for structure in STRUCTURES:
        p=next(p for p in dataset["projects"] if p["structure_type"]==structure and p["split"]=="test")
        source=[r for r in dataset["boq"] if r["synthetic_project_id"]==p["synthetic_project_id"]]
        for kind in ("MissingRequiredItem","ExplicitRecordedZero"):
            vid=p["synthetic_project_id"]+"-"+kind
            project_rows.append({"diagnostic_project_id":vid,"source_project_id":p["synthetic_project_id"],
                "project_type":"廠房","structure_type":structure,"source_split":"test","diagnostic_kind":kind,
                "independent_project":False,"history_eligible":False})
            altered=[]
            for r in source:
                clone=dict(r);clone["synthetic_project_id"]=vid
                clone["synthetic_line_id"]=r["synthetic_line_id"]+"-"+kind
                clone["synthetic_transaction_id"]=""
                if r["material_code"]=="SYN-CONCRETE":
                    if kind=="MissingRequiredItem":continue
                    clone["quantity"]="0.000";clone["amount"]="0.00"
                altered.append(clone)
            rows.extend(altered)
            truth.append({"diagnostic_project_id":vid,"source_project_id":p["synthetic_project_id"],
                "material_code":"SYN-CONCRETE","status":kind,"not_applicable":False,"engineering_anomaly":None,
                "requires_data_review":True,"history_eligible":False,
                "baseline_quantity":str(sum(Decimal(r["quantity"]) for r in source if r["material_code"]=="SYN-CONCRETE"))})
    return project_rows,rows,truth

def validate(dataset):
    ps={p["synthetic_project_id"]:p for p in dataset["projects"]}
    assert len(ps)==180 and all(p["project_type"]=="廠房" and p["data_origin"]=="SYNTHETIC" for p in ps.values())
    assert Counter((p["structure_type"],p["split"]) for p in ps.values()) == Counter({
        (s,split):n for s in STRUCTURES for split,n in [("train",42),("validation",9),("test",9)]})
    assert all(not p["synthetic_project_id"].startswith("SYN-") for p in ps.values())
    tx={t["synthetic_transaction_id"]:t for t in dataset["history"]}
    assert len(tx)==len(dataset["history"])==len(dataset["boq"])
    catalog={m["material_code"]:m for m in dataset["catalog"]}
    by_material=defaultdict(Decimal);ids=set()
    for r in dataset["boq"]:
        assert r["synthetic_line_id"] not in ids;ids.add(r["synthetic_line_id"])
        p=ps[r["synthetic_project_id"]];t=tx[r["synthetic_transaction_id"]]
        assert r["split"]==t["split"]==p["split"] and r["structure_type"]==p["structure_type"]
        assert r["unit"]==catalog[r["material_code"]]["unit"] and r["data_origin"]=="SYNTHETIC"
        assert t["synthetic_project_id"]==r["synthetic_project_id"] and t["material_code"]==r["material_code"]
        q,price=Decimal(r["quantity"]),Decimal(r["unit_price"])
        assert q > 0 and price > 0 and quant(q*price,".01")==Decimal(r["amount"])
        assert t["actual_quantity"]==r["quantity"] and t["unit_price"]==r["unit_price"]
        if r["unit"] in COUNT_UNITS: assert q==int(q)
        by_material[(r["synthetic_project_id"],r["material_code"])]+=q
    assert len(dataset["applicability"])==180*len(catalog)
    for a in dataset["applicability"]:
        quantity=by_material.get((a["synthetic_project_id"],a["material_code"]))
        if a["status"]=="ApplicablePresent":assert quantity==Decimal(a["expected_quantity"])
        else:assert quantity is None and a["expected_quantity"]==""
    for p in ps.values():
        pid=p["synthetic_project_id"]
        assert ((pid,"SYN-STEEL") in by_material)==(p["structure_type"]!="RC")
        assert sum((pid,code_for_id(mid)) in by_material for mid in (5,49))==1
        assert sum((pid,code_for_id(mid)) in by_material for mid in (8,52))==1
        for mid in (6,11,20,22,23,48,51):
            assert ((pid,code_for_id(mid)) in by_material)==(int(p["basement_floor_count"])>0)
    return {"project_count":len(ps),"boq_line_count":len(dataset["boq"]),"transaction_count":len(tx),
        "applicability_rows":len(dataset["applicability"]),
        "status_counts":dict(Counter(a["status"] for a in dataset["applicability"])),
        "materials_per_project":{"min":min(sum(1 for pid,c in by_material if pid==p) for p in ps),
                                 "max":max(sum(1 for pid,c in by_material if pid==p) for p in ps)}}

def write_csv(path,rows,fields):
    with path.open("w",newline="",encoding="utf-8-sig") as handle:
        w=csv.DictWriter(handle,fieldnames=fields,lineterminator="\n");w.writeheader();w.writerows(rows)

def write_dataset(output,seed=20261122):
    output=Path(output)
    if output.exists():raise ValueError("refusing to overwrite any existing dataset directory")
    data=produce(seed);summary=validate(data)
    output.mkdir(parents=True,exist_ok=False)
    files=[
        ("projects.csv",data["projects"],PROJECT_FIELDS),
        ("project_boq_rows.csv",data["boq"],BOQ_FIELDS),
        ("historical_transactions.csv",data["history"],HISTORY_FIELDS),
        ("material_catalog.csv",data["catalog"],list(data["catalog"][0])),
        ("project_material_applicability.csv",data["applicability"],TRUTH_FIELDS)]
    dp,dr,dt=diagnostics(data)
    files.extend([("diagnostic_projects.csv",dp,list(dp[0])),("diagnostic_boq_rows.csv",dr,BOQ_FIELDS),
                  ("diagnostic_truth.csv",dt,list(dt[0]))])
    for name,rows,fields in files:write_csv(output/name,rows,fields)
    (output/"assumptions.json").write_text(json.dumps(ASSUMPTIONS,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    hashes={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(output.iterdir())}
    manifest={"generator_version":VERSION,"dataset_schema_version":SCHEMA,"data_origin":"SYNTHETIC","seed":seed,
        "project_type":"廠房","batch_id":"FACTORY_20261122_V1",**summary,
        "split_counts":{"train":126,"validation":27,"test":27},
        "structure_type_counts":{s:60 for s in STRUCTURES},
        "catalog_material_count":len(data["catalog"]),
        "distinct_material_codes":len({r["material_code"] for r in data["boq"]}),
        "files_sha256":hashes,"diagnostic_cases":len(dp),
        "generator_sha256":hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        "catalog_sha256":hashlib.sha256(CATALOG_PATH.read_bytes()).hexdigest(),
        "schema_changed":False,"database_imported":False,"automatic_approval_allowed":False,
        "scope":"ILLUSTRATIVE_SELECTED_FACTORY_TRADES_NOT_COMPLETE_CONTRACT",
        "limitations":["All quantities, prices and applicability are synthetic assumptions, not engineering observations.",
            "Train/validation/test are grouped by independent project; diagnostics are paired variants, not new independent cases.",
            "Only train may be considered for isolated demo references after scope/mapping review; never import validation/test/diagnostics.",
            "Four new factory catalog entries require explicit mapping; ID hints are not portable database foreign keys.",
            "No residential files or database data are changed.","No real engineering accuracy or normal/abnormal labels are established."]}
    (output/"manifest.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    return manifest

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--output",type=Path,required=True);p.add_argument("--seed",type=int,default=20261122)
    args=p.parse_args()
    try:
        m=write_dataset(args.output,args.seed)
        print(json.dumps({k:m[k] for k in ["generator_version","project_count","split_counts","boq_line_count","materials_per_project","diagnostic_cases"]},ensure_ascii=False))
    except (ValueError,AssertionError,OSError) as e:p.error(str(e))

if __name__=="__main__":main()
