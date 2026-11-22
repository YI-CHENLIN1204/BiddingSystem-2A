"""Summarize lower and upper misses separately for frozen A0/A1 predictions."""
import csv
import sys
from collections import Counter

for level, path in zip((70, 90), sys.argv[1:], strict=True):
    groups = Counter()
    with open(path, encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    assert len(rows) == len({(r["project"], r["scope"]) for r in rows}) == 78
    for row in rows:
        value = float(row["truth"])
        for method in ("a0", "a1"):
            low, high = (float(row[f"{method}_{bound}"]) for bound in ("low", "high"))
            outcome = "below" if value < low else "above" if value > high else "inside"
            assert int(outcome == "inside") == int(row[f"{method}_hit"])
            groups[method, outcome] += 1
    for method in ("a0", "a1"):
        print(level, method, *(groups[method, outcome] for outcome in ("below", "inside", "above")))
