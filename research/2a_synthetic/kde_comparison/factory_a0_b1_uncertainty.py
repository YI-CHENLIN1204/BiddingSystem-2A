"""Paired interval-score bootstrap for A0 versus reconstructed B1."""
import csv
import random
import sys
from collections import defaultdict
from statistics import mean

with open(sys.argv[1], encoding="utf-8-sig", newline="") as stream:
    if len(sys.argv) == 2:
        rows = [r for r in csv.DictReader(stream) if r["split"] == "test"]
    else:
        rows = [r for r in csv.DictReader(stream)
                if r["split"] == "test" and r["method"] == "B1" and r["level"] == "0.9"]
if len(sys.argv) == 3:
    with open(sys.argv[2], encoding="utf-8-sig", newline="") as stream:
        baselines = {(r["project"], r["scope"]): r for r in csv.DictReader(stream)}
    assert len(baselines) == len(rows)
    for row in rows:
        baseline = baselines[row["project"], row["scope"]]
        assert row["structure"] == baseline["structure"]
        assert float(row["truth"]) == float(baseline["truth"])
        row["a_score"] = baseline["a0_score"]
assert len(rows) == len({(r["project"], r["scope"]) for r in rows}) == 78
print("scope,a0_available,b1_available,paired,a0_score,b1_score,difference,ci95_low,ci95_high")
for scope in sorted({r["scope"] for r in rows}):
    cohort = [r for r in rows if r["scope"] == scope]
    strata = defaultdict(list)
    for r in cohort:
        if r["status"] == "ok" and r["a_score"]:
            strata[r["structure"]].append((float(r["a_score"]), float(r["relative_score"])))
    pairs = [pair for group in strata.values() for pair in group]
    rng = random.Random(20261122)
    draws = sorted(mean(b-a for group in strata.values()
                        for a,b in rng.choices(group,k=len(group))) for _ in range(2000))
    a,b = map(mean,zip(*pairs))
    print(f"{scope},{sum(bool(r['a_score']) for r in cohort)},{sum(r['status']=='ok' for r in cohort)},"
          f"{len(pairs)},{a:.6f},{b:.6f},{b-a:.6f},{draws[49]:.6f},{draws[1949]:.6f}")

