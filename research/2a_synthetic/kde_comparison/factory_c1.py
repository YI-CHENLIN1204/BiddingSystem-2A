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


def main(src,out,hold_out_materials=False):
    manifest=src/"manifest.json"
    assert hashlib.sha256(manifest.read_bytes()).hexdigest()=="e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
    raw=src/"project_boq_rows.csv"
    assert hashlib.sha256(raw.read_bytes()).hexdigest()==json.loads(manifest.read_text(encoding="utf8"))["files_sha256"][raw.name]
    rows=read(raw)
    held = set()
    if hold_out_materials:
        codes = defaultdict(set)
        for row in rows:
            if row["split"] == "train":
                codes[row["major_category"]].add(row["material_code"])
        held = {sorted(group)[0] for group in codes.values() if len(group) >= 2}
    removed_names = {normalize(r["item_name"]) for r in rows
                     if r["split"] == "train" and r["material_code"] in held}
    groups=defaultdict(Counter)
    seen=set()
    for r in rows:
        if r["split"]=="train" and normalize(r["item_name"]) not in removed_names:
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
        review_ranks = Counter()
        for r in rows:
            if r["split"]!=split or r["synthetic_project_id"]=="FAC-20261122-SC-0052":
                continue
            text=normalize(r["item_name"])
            if hold_out_materials and (r["material_code"] not in held or text in seen):
                continue
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
            rank = next((i for i, chapter in enumerate(ranked[:3], 1)
                         if chapter == r["major_category"]), None)
            review_ranks[str(rank) if rank else "fallback"] += 1
        summary.append(dict(split=split,n=n,top1=top1,top3=top3,unseen=unseen,unclassified=unknown,
                            review_ranks=dict(review_ranks)))
    filename = "factory_c1_held_materials.json" if hold_out_materials else "factory_c1_summary.json"
    (out/filename).write_text(json.dumps(dict(chapters=len(groups),held=sorted(held),summary=summary),indent=2),encoding="utf8")
    print(summary)


if __name__=="__main__":
    main(Path(sys.argv[1]),Path(sys.argv[2]),"--held-materials" in sys.argv[3:])

