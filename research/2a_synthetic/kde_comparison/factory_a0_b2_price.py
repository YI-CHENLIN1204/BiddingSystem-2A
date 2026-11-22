"""Compare A0 to B2 on the same floor-area denominator and price targets."""
import csv
import random
import sys
from collections import defaultdict
from statistics import mean

def read(path):
    with open(path, encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))

b2 = read(sys.argv[1])
print("level,paired,a0_score,b2_score,difference,ci95_low,ci95_high,a0_hits,b2_hits")
for level, path in zip(("0.7","0.9"), sys.argv[2:], strict=True):
    base = {r["project"]:r for r in read(path) if r["scope"]=="structure_price"}
    strata = defaultdict(list)
    targets = [r for r in b2 if r["split"]=="test" and r["scope"]=="structure_price"
               and r["method"]=="B2" and r["level"]==level]
    assert len(targets)==len(base)==len({r["project"] for r in targets})==26
    for r in targets:
        a = base[r["project"]]
        assert r["status"]=="ok" and r["structure"]==a["structure"]
        assert float(r["truth"])==float(a["truth"])
        strata[r["structure"]].append((float(a["a0_score"]),float(r["relative_score"]),
                                       int(a["a0_hit"]),int(r["covered"])))
    pairs = [pair for group in strata.values() for pair in group]
    a,b = map(mean,zip(*(p[:2] for p in pairs)))
    rng = random.Random(20261122)
    draws = sorted(mean(p[1]-p[0] for group in strata.values()
                        for p in rng.choices(group,k=len(group))) for _ in range(2000))
    print(f"{level},26,{a:.6f},{b:.6f},{b-a:.6f},{draws[49]:.6f},{draws[1949]:.6f},"
          f"{sum(p[2] for p in pairs)},{sum(p[3] for p in pairs)}")
