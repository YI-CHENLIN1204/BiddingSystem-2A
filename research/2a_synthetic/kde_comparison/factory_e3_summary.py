"""Summarize E3 on jointly available predictions with original method rules."""
import csv
import sys
from collections import defaultdict
from pathlib import Path
from statistics import mean

root,out = map(Path,sys.argv[1:])
def read(p):
    with p.open(encoding="utf-8-sig",newline="") as f:
        return list(csv.DictReader(f))
baseline={(r["project"],r["scope"]):r for r in read(root/"factory-b1/factory_a0_past_baseline.csv") if r["split"]=="test"}
b=read(root/"factory-b1-past/factory_b1_pairs.csv")
summary=[]
per_case=[]
for level,suffix in ((.7,""),(.9,"_90pct")):
    a={(r["project"],r["scope"]):r for r in read(root/"factory-b1"/f"past_only_factory_a0_a1{suffix}_pairs.csv")}
    groups=defaultdict(list)
    for row in b:
        if row["split"]!="test" or row["method"]!="B1" or float(row["level"])!=level:
            continue
        key=row["project"],row["scope"]
        hist=baseline[key]
        assert float(hist["truth"])==float(row["truth"])
        truth=float(row["truth"])
        ar=a.get(key)
        br=row if row["status"]=="ok" else None
        if ar:
            assert float(ar["truth"])==truth and ar["refs"]==hist["a_refs"]
        record=dict(project=key[0],scope=key[1],structure=row["structure"],truth=truth,level=level,
                    a0_status="ok" if ar else "insufficient",b1_status=row["status"],
                    a0_refs=hist["a_refs"],b1_refs=row["refs"],b1_n=row["n"])
        for method,r,columns in (("a0",ar,("a0_low","a0_high","a0_score","a0_hit")),
                                 ("b1",br,("low","high","relative_score","covered"))):
            for col,field in zip(("low","high","score","hit"),columns):
                record[method+"_"+col]=float(r[field]) if r else None
            med=(hist["a_median"] if method=="a0" else row["median"]) if r else None
            record[method+"_median"]=float(med) if med is not None else None
            record[method+"_ape"]=abs(float(med)-truth)/truth if med is not None else None
            record[method+"_width"]=(record[method+"_high"]-record[method+"_low"])/truth if r else None
        groups[key[1]].append(record)
        per_case.append(record)
    for scope,rows in sorted(groups.items()):
        assert len(rows)==len({r["project"] for r in rows})==26
        paired=[r for r in rows if r["a0_status"]==r["b1_status"]=="ok"]
        record=dict(scope=scope,level=level,n=26,paired=len(paired))
        for method in ("a0","b1"):
            record[method+"_available"]=sum(r[method+"_status"]=="ok" for r in rows)
            for metric in ("score","hit","width","ape"):
                key=method+"_"+metric
                record[key]=mean(r[key] for r in paired) if paired else None
        summary.append(record)
for name,rows in (("e3_per_case.csv",per_case),("e3_summary.csv",summary)):
    with (out/name).open("w",encoding="utf8",newline="") as f:
        w=csv.DictWriter(f,fieldnames=rows[0]);w.writeheader();w.writerows(rows)
