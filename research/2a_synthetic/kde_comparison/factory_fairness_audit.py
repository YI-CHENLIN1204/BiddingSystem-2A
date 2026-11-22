"""Audit frozen factory comparisons without retuning or changing predictions."""
import csv
import math
import sys
from pathlib import Path

root = Path(sys.argv[1])
def read(name):
    with (root / name).open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))

def index(rows):
    pairs = {(r["project"], r["scope"]): r for r in rows}
    assert len(pairs) == len(rows) == 78
    return pairs

a70 = index(read("factory_a0_a1_pairs.csv"))
a90 = index(read("factory_a0_a1_90pct_pairs.csv"))
b70 = index([r for r in read("factory_a0_b1_pairs.csv") if r["split"] == "test"])
assert a70.keys() == a90.keys() == b70.keys()
assert len({p for p, _ in a70}) == 26
assert all(p != "FAC-20261122-SC-0052" for p, _ in a70)
for key, r in a70.items():
    other, b = a90[key], b70[key]
    for field in ("truth", "structure", "refs", "weights", "factor"):
        assert r[field] == other[field], (key, field)
    assert r["refs"] == b["a_refs"] and r["weights"] == b["a_weights"]
    assert float(r["truth"]) == float(b["truth"])
    assert math.isclose(float(r["a0_score"]), float(b["a_score"]), abs_tol=1e-12)
    refs = r["refs"].split("|")
    weights = list(map(float, r["weights"].split("|")))
    assert len(set(refs)) == len(refs) == len(weights) == 8
    assert key[0] not in refs and all(math.isfinite(w) and w > 0 for w in weights)
    for method in ("a0", "a1"):
        assert float(other[method+"_low"]) <= float(r[method+"_low"])
        assert float(other[method+"_high"]) >= float(r[method+"_high"])
        for record, alpha in ((r,.3),(other,.1)):
            lo, hi = (float(record[method+"_"+q]) for q in ("low","high"))
            y = float(record["truth"])
            assert y > 0 and lo <= hi
            assert int(record[method+"_hit"]) == int(lo <= y <= hi)
            score = (hi-lo+2/alpha*max(lo-y,y-hi,0))/y
            assert math.isclose(score,float(record[method+"_score"]),abs_tol=1e-12)
print("PASS: 78 paired targets, fixed reference inputs, nested intervals, 312 score and coverage checks")
for scope in sorted({s for _,s in b70}):
    rows = [r for (_,s),r in b70.items() if s == scope]
    print(scope, sum(r["status"] == "ok" for r in rows), "/", len(rows), "B1 available")
