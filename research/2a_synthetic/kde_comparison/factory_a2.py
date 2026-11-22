"""Exploratory A2 comparison at the frozen A1 bandwidth."""
import csv
import runpy
import sys
from pathlib import Path
from statistics import mean

run = runpy.run_path(str(Path(__file__).with_name("replay_factory_a1.py")))
alpha = run["alpha"]
groups = run["groups_for"](run["test"])
output = []
for (structure, scope), rows in sorted(groups.items()):
    outcomes = []
    for row in rows:
        weights = row["a_weights"].split("|")
        equal = dict(row, a_weights="|".join("1" for _ in weights))
        bounds = run["interval"](equal, run["factor"], alpha)
        assert bounds is not None
        outcomes.append(run["metrics"](bounds, row["truth"], alpha))
    output.append([structure, scope, len(rows), *map(mean, zip(*outcomes))])
assert sum(row[2] for row in output) == 78
path = Path(sys.argv[2])/f"factory_a2_{round(100*(1-alpha))}.csv"
with path.open("w", encoding="utf8", newline="") as stream:
    writer = csv.writer(stream)
    writer.writerow(["structure","scope","n","relative_score","coverage","relative_width"])
    writer.writerows(output)
print("A2", *map(mean, zip(*(row[3:] for row in output))))
