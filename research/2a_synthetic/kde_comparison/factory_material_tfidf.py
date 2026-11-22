"""Material-ID candidate benchmark with explicit safe-release labels."""
import csv
import hashlib
import json
import math
import sys
from collections import Counter, defaultdict
from pathlib import Path

from factory_c1 import grams
from factory_diagnostics_perf import read
from replay_factory_dictionary import normalize

source, output = map(Path,sys.argv[1:])
manifest=(source/"manifest.json").read_bytes()
assert hashlib.sha256(manifest).hexdigest()=="e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
path=source/"project_boq_rows.csv"
assert hashlib.sha256(path.read_bytes()).hexdigest()==json.loads(manifest)["files_sha256"][path.name]
rows=read(path)
terms=defaultdict(set)
signatures=defaultdict(set)
docs=defaultdict(Counter)
fields=("unit","spec","price_scope","major_category")
for r in rows:
    if r["split"]=="train":
        name=normalize(r["item_name"])
        code=r["material_code"]
        terms[name].add(code)
        signatures[name].add(tuple(r[k] for k in fields))
        docs[code].update(grams(name))
freq=Counter(word for doc in docs.values() for word in doc)
idf={term:1+math.log((len(docs)+1)/(count+1)) for term,count in freq.items()}
def vector(counts):
    v={k:n*idf[k] for k,n in counts.items() if k in idf}
    norm=math.sqrt(sum(x*x for x in v.values()))
    return {k:x/norm for k,x in v.items()} if norm else {}
vectors={k:vector(doc) for k,doc in docs.items()}
ordered=sorted(terms,key=lambda x:(-len(x),x))
rows_out=[]
for split in ("validation","test"):
    targets=[r for r in rows if r["split"]==split and r["synthetic_project_id"]!="FAC-20261122-SC-0052"
             and r["material_code"] in ("SYN-REBAR","SYN-CONCRETE")]
    for r in targets:
        name=normalize(r["item_name"])
        code=r["material_code"]
        variants=[("control",name,code,{}),("vendor_prefix","甲商工程 "+name,code,{}),
                  ("alias","預拌砼" if code=="SYN-CONCRETE" else "鋼質補強筋",code,{}),
                  ("simplified",name.translate(str.maketrans("鋼筋預拌混凝土","钢筋预拌混凝土")),code,{}),
                  ("same_name_other_material",name,"SYN-REBAR" if code=="SYN-CONCRETE" else "SYN-CONCRETE",{}),
                  ("unknown_item","無對照陌生項目 XQ-77","HELD_UNKNOWN",{}),
                  ("different_spec",name,code,{"spec":"UNSEEN-SPEC"}),
                  ("wrong_unit",name,code,{"unit":"kg" if r["unit"]!="kg" else "m3"})]
        for variant,shown,truth,changes in variants:
            item={**r,**changes}
            safe=variant in ("control","vendor_prefix","alias","simplified") and item["price_scope"]=="MaterialOnly"
            exact=terms.get(shown,set())
            contained=[term for term in ordered if term in shown or term in normalize(item["spec"])]
            longest=[term for term in contained if len(term)==len(contained[0])]
            contains=set().union(*(terms[t] for t in longest))
            v=vector(grams(shown))
            ranked=sorted(((sum(w*ref.get(k,0) for k,w in v.items()), material)
                       for material,ref in vectors.items()),key=lambda x:(-x[0],x[1]))
            candidates=[material for score,material in ranked[:3] if score>0]
            guarded=exact if len(exact)==1 and item["price_scope"]=="MaterialOnly" and float(item["quantity"])>0 and tuple(item[k] for k in fields) in signatures[shown] else set()
            for method,found in (("exact",exact),("contains",contains),("guarded",guarded),("material_tfidf",set())):
                match=next(iter(found)) if len(found)==1 else ""
                rows_out.append(dict(split=split,project=r["synthetic_project_id"],line=r["synthetic_line_id"],
                    variant=variant,method=method,truth=truth,prediction=match,
                    correct=int(match==truth),wrong=int(bool(match) and match!=truth),
                    pending=int(not match),safe_release=int(match==truth and safe),
                    unsafe_release=int(bool(match) and (match!=truth or not safe)),
                    unknown=int(shown not in terms),conflict=int(len(found)>1),
                    top1=int(bool(candidates) and candidates[0]==truth) if method=="material_tfidf" else "",
                    top3=int(truth in candidates) if method=="material_tfidf" else ""))
def write(path,values):
    with path.open("w",encoding="utf8",newline="") as stream:
        w=csv.DictWriter(stream,fieldnames=values[0]);w.writeheader();w.writerows(values)
write(output/"dictionary_material_per_case.csv",rows_out)
summary=[]
keys=sorted({(r["split"],r["variant"],r["method"]) for r in rows_out})
for split,variant,method in keys:
    entries=[r for r in rows_out if (r["split"],r["variant"],r["method"])==(split,variant,method)]
    metrics={k:sum(r[k] for r in entries) for k in ("correct","wrong","pending","safe_release","unsafe_release","unknown","conflict")}
    assert metrics["correct"]+metrics["wrong"]+metrics["pending"]==len(entries)
    summary.append(dict(split=split,variant=variant,method=method,n=len(entries),**metrics,
        top1=sum(r["top1"] for r in entries) if method=="material_tfidf" else "",
        top3=sum(r["top3"] for r in entries) if method=="material_tfidf" else ""))
write(output/"dictionary_material_summary.csv",summary)
