"""Offline, train-only material mapping and a controlled stress panel."""
import csv
import hashlib
import json
import math
import sys
from collections import Counter, defaultdict
from pathlib import Path

from replay_factory_dictionary import normalize

EXCLUDED = "FAC-20261122-SC-0052"
FIELDS = ("unit", "spec", "price_scope", "major_category")
PROFILES = {"conservative": (15,10,180,3600,300), "middle": (30,6,120,1800,180),
            "optimistic": (60,3,60,900,60)}


def main(source, dest):
    manifest = source / "manifest.json"
    assert hashlib.sha256(manifest.read_bytes()).hexdigest() == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
    for name, expected in json.loads(manifest.read_text(encoding="utf8"))["files_sha256"].items():
        assert hashlib.sha256((source/name).read_bytes()).hexdigest() == expected
    with (source/"project_boq_rows.csv").open(encoding="utf-8-sig") as stream:
        rows=list(csv.DictReader(stream))
    dest.mkdir(parents=True, exist_ok=True)
    terms=defaultdict(set)
    signatures=defaultdict(set)
    for r in rows:
        if r["split"]=="train":
            term=normalize(r["item_name"])
            terms[term].add(r["material_code"])
            signatures[term].add(tuple(r[f] for f in FIELDS))
    order=sorted(terms,key=lambda k:(-len(k),k))
    def classify(r):
        name=normalize(r["item_name"])
        spec=normalize(r["spec"])
        term=next((k for k in order if k in name or k in spec),None)
        if term is None:
            return None,None
        pred=sorted(terms[term])[0]
        try:
            quantity=float(r["quantity"])
        except ValueError:
            quantity=0
        can_pass=(term==name and len(terms[term])==1 and math.isfinite(quantity) and quantity>0
                  and r["price_scope"]=="MaterialOnly" and tuple(r[f] for f in FIELDS) in signatures[term])
        return pred,pred if can_pass else None
    def write(name,data):
        with (dest/name).open("w",encoding="utf-8-sig",newline="") as out:
            w=csv.DictWriter(out,fieldnames=data[0])
            w.writeheader()
            w.writerows(data)
    summaries=[]
    stress=[]
    labor=[]
    breakdown=defaultdict(Counter)
    for split in ("validation","test"):
        targets=[r for r in rows if r["split"]==split and r["synthetic_project_id"]!=EXCLUDED]
        counts=[0,0,0]
        contains_correct=0
        contains_auto=0
        for r in targets:
            pred,safe=classify(r)
            contains_auto+=pred is not None
            contains_correct+=pred==r["material_code"]
            counts[1 if safe is None else 0 if safe==r["material_code"] else 2]+=1
            exact = terms.get(normalize(r["item_name"]), set())
            for field in FIELDS:
                for method, prediction in (
                    ("zero", None), ("exact", next(iter(exact)) if len(exact)==1 else None),
                    ("contains", pred), ("guarded", safe)
                ):
                    outcome = "pending" if prediction is None else "correct" if prediction==r["material_code"] else "wrong"
                    breakdown[split,field,r[field],method][outcome]+=1
        n=len(targets)
        c,m,w=counts
        assert n==c+m+w
        summaries.append(dict(split=split,n=n,contains_auto=contains_auto,contains_correct=contains_correct,
                              policy_accepted_correct=c,manual=m,wrong=w))
        for scenario,(manual,quick,fix,setup,maintenance) in PROFILES.items():
            for size in (100,500,1000):
                for amortize in (1,20):
                    base=size*manual
                    assisted=size/n*(c*quick+m*manual+w*(quick+fix))+setup/amortize+maintenance
                    labor.append(dict(split=split,scenario=scenario,rows=size,amortize=amortize,
                                      manual_seconds=manual,quick_seconds=quick,extra_fix_seconds=fix,
                                      setup_seconds=setup,maintenance_seconds=maintenance,
                                      baseline_minutes=base/60,assisted_minutes=assisted/60,
                                      saving_minutes=(base-assisted)/60))
        ids=sorted({r["synthetic_project_id"] for r in targets})
        for i in ids:
            r=min((r for r in targets if r["synthetic_project_id"]==i and r["material_code"]=="SYN-REBAR"),key=lambda r:r["synthetic_line_id"])
            for case,changes in (("control",{}),("dimension",{"unit":"m3"}),("unseen_grade",{"spec":"SD490 / UNVERIFIED"}),
                                 ("labor_only",{"price_scope":"LaborOnly"}),("zero",{"quantity":"0"}),
                                 ("negative",{"quantity":"-1"}),("missing",{"quantity":""}),
                                 ("freight",{"item_name":r["item_name"]+" 運費"})):
                pred,safe=classify({**r,**changes})
                stress.append(dict(split=split,project=i,source_line=r["synthetic_line_id"],case=case,
                                   review_required=case!="control",contains_auto=pred is not None,guard_auto=safe is not None))
    write("factory_mapping_safety.csv",summaries)
    write("factory_mapping_strata.csv", [
        dict(split=split,field=field,value=value,method=method,total=c.total(),
             correct=c["correct"],wrong=c["wrong"],pending=c["pending"])
        for (split,field,value,method),c in sorted(breakdown.items())
    ])
    write("factory_safety_cases.csv",stress)
    write("labor_savings_scenarios.csv",labor)
    print(json.dumps(summaries))


if __name__=="__main__":
    main(Path(sys.argv[1]),Path(sys.argv[2]))

