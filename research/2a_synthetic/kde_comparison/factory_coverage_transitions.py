"""Compare paired coverage transitions in frozen factory interval snapshots."""
import csv
import sys
from collections import Counter

for level, path in zip((70, 90), sys.argv[1:], strict=True):
    with open(path, encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    assert len(rows) == len({(r["project"], r["scope"]) for r in rows}) == 78
    for scope in ("all", *sorted({r["scope"] for r in rows})):
        counts = Counter((int(r["a0_hit"]), int(r["a1_hit"])) for r in rows
                         if scope == "all" or r["scope"] == scope)
        print(level, scope, *(counts[k] for k in ((1,1),(0,1),(1,0),(0,0))))
