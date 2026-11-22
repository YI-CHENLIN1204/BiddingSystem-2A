"""Factory C1 reconstruction with train-only chapter vectors and an empty keyword list."""
import json
import math
import sys
from collections import Counter, defaultdict
from pathlib import Path
from replay_factory_dictionary import normalize
from factory_diagnostics_perf import read
import hashlib


def grams(s):
    s=normalize(s)
    return Counter(s[i:i+n] for n in (1,2,3) for i in range(len(s)-n+1))


def main(src,out):
    manifest=src/"manifest.json"
    assert hashlib.sha256(manifest.read_bytes()).hexdigest()=="e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
    raw=src/"project_boq_rows.csv"
    assert hashlib.sha256(raw.read_bytes()).hexdigest()==json.loads(manifest.read_text(encoding="utf8"))["files_sha256"][raw.name]
    rows=read(raw)
    groups=defaultdict(Counter)
    seen=set()
    for r in rows:
        if r["split"]=="train":
            groups[r["major_category"]].update(grams(r["item_name"]))
            seen.add(normalize(r["item_name"]))
    freq=Counter(k for v in groups.values() for k in v)
    idf={k:1+math.log((len(groups)+1)/(n+1)) for k,n in freq.items()}
    def vector(counts):
        v={k:n*idf[k]/counts.total() for k,n in counts.items() if k in idf}
        norm=math.sqrt(sum(x*x for x in v.values()))
        return {k:x/norm for k,x in v.items()} if norm else {}
    chapters={k:vector(v) for k,v in groups.items()}
    scores={}
    summary=[]
    for split in ("validation","test"):
        n=top1=top3=unseen=unknown=0
        for r in rows:
            if r["split"]!=split or r["synthetic_project_id"]=="FAC-20261122-SC-0052":
                continue
            text=normalize(r["item_name"])
            if text not in scores:
                v=vector(grams(text))
                ranking=sorted(((round(100*sum(x*w.get(k,0) for k,x in v.items()),1),c) for c,w in chapters.items()),key=lambda t:(-t[0],t[1]))
                scores[text]=[c for value,c in ranking if value>0]
            ranked=scores[text]
            n+=1
            top1+=r["major_category"] in ranked[:1]
            top3+=r["major_category"] in ranked[:3]
            unseen+=text not in seen
            unknown+=not ranked
        summary.append(dict(split=split,n=n,top1=top1,top3=top3,unseen=unseen,unclassified=unknown))
    (out/"factory_c1_summary.json").write_text(json.dumps(dict(chapters=len(groups),summary=summary),indent=2),encoding="utf8")
    print(summary)


if __name__=="__main__":
    main(Path(sys.argv[1]),Path(sys.argv[2]))
