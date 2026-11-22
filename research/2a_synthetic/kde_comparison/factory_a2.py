"""Exploratory A2 comparison at the frozen A1 bandwidth."""
import csv
import random
from collections import defaultdict
import runpy
import sys
from pathlib import Path
from statistics import mean

run = runpy.run_path(str(Path(__file__).with_name("replay_factory_a1.py")))
alpha = run["alpha"]
groups = run["groups_for"](run["test"])
output = []
differences = defaultdict(dict)
baselines = {(r["project"], r["scope"]): r for r in run["results"]}
for (structure, scope), rows in sorted(groups.items()):
    outcomes = []
    for row in rows:
        weights = row["a_weights"].split("|")
        equal = dict(row, a_weights="|".join("1" for _ in weights))
        bounds = run["interval"](equal, run["factor"], alpha)
        assert bounds is not None
        metrics = run["metrics"](bounds, row["truth"], alpha)
        outcomes.append(metrics)
        base = baselines[row["project"], scope]
        project = differences[structure].setdefault(row["project"], [])
        project.append(tuple(metrics[0] - base[m+"_score"] for m in ("a0","a1")))
    output.append([structure, scope, len(rows), *map(mean, zip(*outcomes))])
assert sum(row[2] for row in output) == 78
path = Path(sys.argv[2])/f"factory_a2_{round(100*(1-alpha))}.csv"
with path.open("w", encoding="utf8", newline="") as stream:
    writer = csv.writer(stream)
    writer.writerow(["structure","scope","n","relative_score","coverage","relative_width"])
    writer.writerows(output)
print("A2", *map(mean, zip(*(row[3:] for row in output))))
cohorts = []
for projects in differences.values():
    assert all(len(values) == 3 for values in projects.values())
    cohorts.append([tuple(map(mean, zip(*values))) for values in projects.values()])
rng = random.Random(20261122)
draws = [[], []]
for _ in range(2000):
    sample = [rng.choices(group, k=len(group)) for group in cohorts]
    for i in range(2):
        draws[i].append(mean(mean(pair[i] for pair in group) for group in sample))
for i, method in enumerate(("a0", "a1")):
    values = sorted(draws[i])
    point = mean(mean(pair[i] for pair in group) for group in cohorts)
    print(f"A2 minus {method}: {point:.6f} [{values[49]:.6f}, {values[1949]:.6f}]")

