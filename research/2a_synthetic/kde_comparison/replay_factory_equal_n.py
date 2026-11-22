"""Matched-scope factory E2 replay; freeze parameters before scoring."""
import csv
import hashlib
import json
import math
import random
import sys
from collections import defaultdict
from functools import lru_cache
from pathlib import Path
from statistics import mean
from time import perf_counter

from replay_factory_b1 import cluster, features
from probe_external_report import critical_bandwidth, distance, quantile

source, output = map(Path, sys.argv[1:])
output.mkdir(exist_ok=False, parents=True)
digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
manifest = source/"manifest.json"
assert digest(manifest) == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
for name, expected in json.loads(manifest.read_text(encoding="utf8"))["files_sha256"].items():
    assert digest(source/name) == expected
def read(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))
def write(name, rows):
    with (output/name).open("w", encoding="utf8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=rows[0])
        writer.writeheader()
        writer.writerows(rows)

projects = {p["synthetic_project_id"]:p for p in read(source/"projects.csv")}
train = {i for i,p in projects.items() if p["split"]=="train"}
assert len(projects)==180 and len(train)==126
f = {i:features(p) for i,p in projects.items()}
dates = {i:p["start_date"] for i,p in projects.items()}
from datetime import date
days = {i:date.fromisoformat(d) for i,d in dates.items()}
totals = defaultdict(lambda: defaultdict(float))
for r in read(source/"project_boq_rows.csv"):
    i = r["synthetic_project_id"]
    if r["major_category"] == "結構體工程":
        totals[i][("structure_price","","TWD","chapter_total")] += float(r["amount"])
    if r["material_code"] in ("SYN-CONCRETE","SYN-REBAR") and r["price_scope"]=="MaterialOnly":
        key = tuple(r[k] for k in ("material_code","spec","unit","price_scope"))
        totals[i][key] += float(r["quantity"])

def similarity(i,j):
    a,b = f[i],f[j]
    rel = lambda k:max(0,1-abs(b[k]-a[k])/a[k])
    return round(25*(a[2]==b[2])+30*rel(0)+10*rel(5)
        +15*max(0,1-.1*abs(a[3]-b[3])-.15*abs(a[4]-b[4]))
        +10*max(0,1-abs(a[7]-b[7])/20)
        +10*(projects[i]["city"]==projects[j]["city"]),2)

def empirical(samples,q):
    cutoff = q*sum(w for x,w in samples)
    total = 0
    for x,w in sorted(samples):
        total += w
        if total >= cutoff:
            return x
    return max(x for x,w in samples)

@lru_cache(None)
def groups(pool):
    return cluster(pool,f,4) if len(pool)>=4 else []

freeze = dict(source_sha256=digest(manifest), program_sha256=digest(Path(__file__)),
              groups=4,k=[5,8], interval=[.7,.9], time="strictly earlier start_date proxy",
              tie="stable synthetic ID", price="same structural chapter total / floor area",
              quantity="exact material code, synthetic spec, unit and scope; floor area",
              b1_bandwidth="critical single-mode bandwidth from frozen probe; no test tuning",
              b1_group="nearest group before matched scope filter; no fallback",
              a0="similarity first, then final weight, then ID; SRC-only for SRC target",
              assumptions="exploratory reused synthetic test; no true availability dates")
(output/"manifest.json").write_text(json.dumps(freeze,indent=2),encoding="utf8")
records=[]
for split in ("validation","test"):
    for i in sorted(projects):
        if projects[i]["split"]!=split or i=="FAC-20261122-SC-0052":
            continue
        pool=tuple(sorted(j for j in train if dates[j]<dates[i]))
        partition=groups(pool)
        nearest=min(partition,key=lambda g:(mean(distance(f[i],f[j]) for j in g),g),default=())
        for scope,truth in sorted(totals[i].items()):
            assert truth>0 and math.isfinite(truth)
            scores={j:similarity(i,j) for j in pool if f[j][1]==f[i][1]
                    and (f[i][2]!="SRC" or f[j][2]=="SRC") and totals[j].get(scope,0)>0}
            weights={j:s/100*.5**(abs((days[i]-days[j]).days)/365.2425/3)
                     for j,s in scores.items() if s>0}
            a=sorted(weights,key=lambda j:(-scores[j],-weights[j],j))
            b=sorted((j for j in nearest if totals[j].get(scope,0)>0),
                     key=lambda j:(distance(f[i],f[j]),j))
            for k in (5,8):
                for method,ranked in (("A0-K",a),("B1-K",b)):
                    refs=ranked[:k]
                    assert len(set(refs))==len(refs) and all(j in pool and j in train for j in refs)
                    assert all(totals[j][scope]>0 and dates[j]<dates[i] for j in refs)
                    xs=[totals[j][scope]/f[j][0] for j in refs]
                    status="insufficient_k" if len(refs)<k else "ok"
                    qs={}
                    if status=="ok":
                        if method=="A0-K":
                            qs={q:empirical(list(zip(xs,[weights[j] for j in refs])),q)
                                for q in (.05,.15,.5,.85,.95)}
                        else:
                            h=critical_bandwidth(xs)
                            if h:
                                qs={q:quantile(xs,h,q) for q in (.05,.15,.5,.85,.95)}
                            else:
                                status="zero_span"
                    for level,l,u in ((.7,.15,.85),(.9,.05,.95)):
                        row=dict(project=i,split=split,structure=f[i][2],scope=scope[0],
                                 spec=scope[1],unit=scope[2],work_scope=scope[3],truth=truth,
                                 k=k,level=level,method=method,candidate_n=len(pool),
                                 source_sha256=freeze["source_sha256"],
                                 refs="|".join(refs),n=len(refs),status=status,
                                 low=None,median=None,high=None,score=None,hit=None,width=None,ape=None)
                        if status=="ok":
                            assert len(refs)==k
                            lo,mid,hi=[qs[q]*f[i][0] for q in (l,.5,u)]
                            row.update(low=lo,median=mid,high=hi,
                                score=(hi-lo+2/(1-level)*max(lo-truth,truth-hi,0))/truth,
                                hit=int(lo<=truth<=hi),width=(hi-lo)/truth,ape=abs(mid-truth)/truth)
                        records.append(row)
write("equal_sample_per_case.csv",records)
summary=[]
for split in ("validation","test"):
    for k in (5,8):
        for level in (.7,.9):
            for scope in sorted({r["scope"] for r in records}):
                pairs=defaultdict(dict)
                for r in records:
                    if (r["split"],r["k"],r["level"],r["scope"])==(split,k,level,scope):
                        pairs[r["project"]][r["method"]]=r
                assert len(pairs)==(27 if split=="validation" else 26)
                a,b=zip(*[(p["A0-K"],p["B1-K"]) for p in pairs.values()])
                paired=[(x,y) for x,y in zip(a,b) if x["status"]==y["status"]=="ok"]
                assert all(x["n"]==y["n"]==k and x["truth"]==y["truth"] for x,y in paired)
                row=dict(split=split,k=k,level=level,scope=scope,n=len(a),
                         a0_available=sum(x["status"]=="ok" for x in a),
                         b1_available=sum(y["status"]=="ok" for y in b),paired=len(paired))
                for idx,label in enumerate(("a0","b1")):
                    for metric in ("score","hit","width","ape"):
                        row[label+"_"+metric]=mean(p[idx][metric] for p in paired) if paired else None
                summary.append(row)
write("equal_sample_summary.csv",summary)
(output/"equal_sample_summary.json").write_text(json.dumps(summary,indent=2),encoding="utf8")
for row in summary:
    if row["split"]=="test": print(row)
