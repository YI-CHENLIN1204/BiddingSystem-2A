"""Paired, structure-stratified bootstrap of the factory price denominator study."""
import csv
import random
import sys
from collections import defaultdict
from statistics import mean

with open(sys.argv[1], encoding="utf8", newline="") as stream:
    rows = [r for r in csv.DictReader(stream)
            if r["split"] == "test" and r["scope"] == "structure_price"]
for level in ("0.7", "0.9"):
    pairs = defaultdict(dict)
    for row in rows:
        if row["level"] == level:
            assert row["status"] == "ok"
            pairs[row["structure"], row["project"]][row["method"]] = row
    strata = defaultdict(list)
    for (structure, _), pair in sorted(pairs.items()):
        a, b = pair["B1"], pair["B2"]
        assert (a["truth"], a["refs"]) == (b["truth"], b["refs"])
        strata[structure].append((float(b["relative_score"]) - float(a["relative_score"]),
                                  int(b["covered"]) - int(a["covered"])))
    assert sum(map(len, strata.values())) == 26
    rng = random.Random(20261122)
    samples = [tuple(r for group in strata.values()
                     for r in rng.choices(group, k=len(group))) for _ in range(2000)]
    for i, label in enumerate(("relative_score", "coverage")):
        observed = mean(r[i] for group in strata.values() for r in group)
        draws = sorted(mean(r[i] for r in sample) for sample in samples)
        print(level, label, round(observed, 5), round(draws[49], 5), round(draws[1949], 5))
