"""Reconcile blind judgments and score model outputs after controlled unblinding."""
import argparse
from collections import Counter
import json
from pathlib import Path
from intake import LINES, digest, index, opaque, read, require, write

KEY=("case_id","line_id")
GOLD=KEY+("estimable","material_code","spec_code","unit_code","quantity_status","price_status","should_abstain","reason_code")
PRED=KEY+("dictionary_material","candidates","quantity_status","price_status","abstained","review_seconds","corrections")
CLASSES={"normal","anomaly","inconclusive"}

def validate(row):
    require(all(opaque(v) for v in row.values()),"Use coded labels")
    require(row["estimable"] in {"yes","no","unknown"} and row["should_abstain"] in {"yes","no","unknown"},"Invalid eligibility")
    require(all(row[k] in CLASSES for k in ("quantity_status","price_status")),"Invalid anomaly status")

def reconcile(blind,left,right,out,decisions):
    expected=index(read(blind,LINES),KEY)
    a,b=(index(read(p,GOLD),KEY) for p in (left,right))
    require(expected and a.keys()==b.keys()==expected.keys(),"Both reviews must cover every frozen blind line")
    for row in [*a.values(),*b.values()]:
        validate(row)
    conflicts={k for k in a if any(a[k][f]!=b[k][f] for f in GOLD[2:-1])}
    d=index(read(decisions,GOLD),KEY) if decisions else {}
    require(d.keys()<=conflicts,"Only disputed rows can be adjudicated")
    for row in d.values():
        validate(row)
    out.mkdir()
    write(out/"reviewer-1.csv",GOLD,a.values())
    write(out/"reviewer-2.csv",GOLD,b.values())
    write(out/"disputes-1.csv",GOLD,[a[k] for k in a if k in conflicts])
    write(out/"disputes-2.csv",GOLD,[b[k] for k in b if k in conflicts])
    if decisions:
        write(out/"decisions.csv",GOLD,d.values())
    pending=conflicts-d.keys()
    if not pending:
        write(out/"adjudicated.csv",GOLD,[d.get(k,r) for k,r in a.items()])
    paths=[blind,left,right]+([decisions] if decisions else [])
    (out/"receipt.json").write_text(json.dumps(dict(rows=len(expected),disputed=len(conflicts),
       unresolved=len(pending),input_sha256=[digest(p) for p in paths]),indent=2),encoding="utf-8")

def score(gold,pred,expected_prediction_sha,out):
    require(digest(pred)==expected_prediction_sha.lower(),"Prediction SHA differs from sealed record")
    truth,predictions=(index(read(p,cols),KEY) for p,cols in ((gold,GOLD),(pred,PRED)))
    require(truth and truth.keys()==predictions.keys(),"Predictions must cover every line, including abstentions")
    matrix={k:Counter() for k in ("quantity_status","price_status")}
    rows=[]
    for key,g in truth.items():
        validate(g)
        p=predictions[key]
        require(p["abstained"] in {"yes","no"} and all(p[k] in CLASSES for k in matrix),"Invalid prediction status")
        allowed=g["estimable"]=="yes" and g["material_code"]!="NA"
        candidates=[] if p["candidates"]=="NA" else p["candidates"].split("|")
        require(len(candidates)<=5 and len(set(candidates))==len(candidates) and all(opaque(x) for x in candidates),"Invalid Top-5 list")
        import math
        seconds=float(p["review_seconds"])
        corrections=int(p["corrections"])
        require(math.isfinite(seconds) and seconds>=0 and corrections>=0,"Invalid review burden")
        d=dict(zip(KEY,key))
        for field,counts in matrix.items():
            actual,predicted=g[field],p[field]
            if actual=="inconclusive":
                status="excluded_truth"
            elif p["abstained"]=="yes" or predicted=="inconclusive":
                status="abstain_"+actual
            else:
                status={("anomaly","anomaly"):"tp",("normal","anomaly"):"fp",("anomaly","normal"):"fn",("normal","normal"):"tn"}[actual,predicted]
            counts[status]+=1
            d[field]=status
        d.update(dictionary_correct=int(p["dictionary_material"]==g["material_code"]) if allowed and p["dictionary_material"]!="NA" else "",
                 top1=int(g["material_code"] in candidates[:1]) if allowed else "",
                 top3=int(g["material_code"] in candidates[:3]) if allowed else "",
                 top5=int(g["material_code"] in candidates[:5]) if allowed else "",
                 review_seconds=seconds,corrections=corrections,
                 abstention_agreement=int(p["abstained"]==g["should_abstain"]) if g["should_abstain"]!="unknown" else "")
        rows.append(d)
    def ratio(a,b):
        return a/b if b else None
    metrics={}
    for field,c in matrix.items():
        metrics[field]=dict(counts=dict(c),precision=ratio(c["tp"],c["tp"]+c["fp"]),
            recall_on_decisions=ratio(c["tp"],c["tp"]+c["fn"]),
            recall_including_abstention=ratio(c["tp"],c["tp"]+c["fn"]+c["abstain_anomaly"]),
            fpr_on_decisions=ratio(c["fp"],c["fp"]+c["tn"]))
    for field in ("dictionary_correct","top1","top3","top5","abstention_agreement"):
        values=[x[field] for x in rows if x[field]!=""]
        metrics[field]=dict(n=len(values),rate=ratio(sum(values),len(values)))
    metrics["review_seconds_total"]=sum(x["review_seconds"] for x in rows)
    metrics["corrections_total"]=sum(x["corrections"] for x in rows)
    out.mkdir()
    write(out/"scored-lines.csv",tuple(rows[0]),rows)
    (out/"metrics.json").write_text(json.dumps(dict(gold_sha256=digest(gold),
        predictions_sha256=digest(pred),metrics=metrics),indent=2),encoding="utf-8")

if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__)
    subs=p.add_subparsers(dest="command",required=True)
    r=subs.add_parser("reconcile")
    for f in ("blind","left","right","out"): r.add_argument(f,type=Path)
    r.add_argument("--decisions",type=Path)
    s=subs.add_parser("score")
    for f in ("gold","pred","out"): s.add_argument(f,type=Path)
    s.add_argument("--prediction-sha256",required=True)
    a=p.parse_args()
    if a.command=="reconcile": reconcile(a.blind,a.left,a.right,a.out,a.decisions)
    else: score(a.gold,a.pred,a.prediction_sha256,a.out)
