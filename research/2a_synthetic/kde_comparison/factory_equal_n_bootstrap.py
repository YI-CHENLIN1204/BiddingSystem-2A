"""Paired project bootstrap for E2, excluding single-project cells."""
import csv
import random
import sys
from collections import defaultdict
from pathlib import Path
from statistics import mean

root = Path(sys.argv[1])
with (root/"equal_sample_per_case.csv").open(encoding="utf8",newline="") as stream:
    cells = defaultdict(lambda: defaultdict(dict))
    for r in csv.DictReader(stream):
        if r["split"]=="test":
            cells[r["k"],r["level"],r["scope"]][r["project"]][r["method"]]=r
with (root/"equal_sample_bootstrap.csv").open("w",encoding="utf8",newline="") as stream:
    w=csv.writer(stream)
    w.writerow(["k","level","scope","n","b1_minus_a0","ci95_low","ci95_high","min_structure_n"])
    for key,projects in sorted(cells.items()):
        strata=defaultdict(list)
        for project in sorted(projects):
            a,b=(projects[project][m] for m in ("A0-K","B1-K"))
            if a["status"]==b["status"]=="ok":
                assert a["structure"]==b["structure"] and int(a["n"])==int(b["n"])==int(key[0])
                strata[a["structure"]].append(float(b["score"])-float(a["score"]))
        paired=[x for group in strata.values() for x in group]
        rng=random.Random(20261122)
        low=high=None
        if len(paired)>=5 and min(map(len,strata.values()))>=2:
            draws=sorted(mean(x for group in strata.values() for x in rng.choices(group,k=len(group))) for _ in range(2000))
            low,high=draws[49],draws[1949]
        w.writerow([*key,len(paired),mean(paired) if paired else None,low,high,min(map(len,strata.values()),default=0)])
