"""Measure fixed-interval sensitivity to controlled changes in observed values."""
import csv
import sys
from pathlib import Path

source, output = map(Path, sys.argv[1:])
with source.open(encoding="utf8", newline="") as stream:
    cases = list(csv.DictReader(stream))
assert len(cases) == len({(r["project"], r["scope"]) for r in cases}) == 78
rows = []
for case in cases:
    for multiplier in (0.5, 0.75, 1., 1.25, 1.5):
        value = float(case["truth"]) * multiplier
        for method in ("a0", "a1"):
            low, high = (float(case[f"{method}_{bound}"]) for bound in ("low", "high"))
            assert low <= high
            direction = "below" if value < low else "above" if value > high else "inside"
            rows.append(dict(project=case["project"], scope=case["scope"], multiplier=multiplier,
                             method=method, value=value, low=low, high=high, direction=direction))
with output.open("w", encoding="utf8", newline="") as stream:
    writer = csv.DictWriter(stream, fieldnames=rows[0])
    writer.writeheader()
    writer.writerows(rows)
for m in (0.5, 0.75, 1., 1.25, 1.5):
    direction = "below" if m < 1 else "above" if m > 1 else "inside"
    print(m, direction, [sum(r["direction"] == direction for r in rows
                             if r["multiplier"] == m and r["method"] == method)
                         for method in ("a0", "a1")])
