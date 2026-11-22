"""Bootstrap paired coverage and width differences by whole factory project."""
import csv
import random
import sys
from collections import defaultdict
from statistics import mean

print("level,metric,difference,ci95_low,ci95_high")
for level, path in zip((70, 90), sys.argv[1:], strict=True):
    strata = defaultdict(lambda: defaultdict(list))
    with open(path, encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            strata[row["structure"]][row["project"]].append(
                [float(row["a1_"+m])-float(row["a0_"+m]) for m in ("hit","width")])
    groups = [[tuple(map(mean, zip(*project))) for project in projects.values()]
              for projects in strata.values()]
    assert sorted(map(len,groups)) == [8,9,9]
    assert all(len(rows)==3 for projects in strata.values() for rows in projects.values())
    rng = random.Random(20261122)
    def macro(sample,i):
        return mean(mean(row[i] for row in group) for group in sample)
    draws = [[],[]]
    for _ in range(2000):
        sample = [rng.choices(group,k=len(group)) for group in groups]
        for i in range(2):
            draws[i].append(macro(sample,i))
    for i,name in enumerate(("coverage","relative_width")):
        values = sorted(draws[i])
        print(f"{level},{name},{macro(groups,i):.6f},{values[49]:.6f},{values[1949]:.6f}")
