"""Secondary T05 baseline on the exact same A0 reference IDs."""
import csv
import runpy
import sys
from collections import defaultdict
from pathlib import Path
from statistics import mean

run = runpy.run_path(str(Path(__file__).with_name("replay_factory_a1.py")))
alpha = run["alpha"]
baseline = run["baseline"]
groups = defaultdict(list)
results = []
for row in run["test"]:
    ids = row["a_refs"].split("|")
    scope = row["scope"]
    area = baseline["area"]
    projects = baseline["p"]
    totals = baseline["tot"]
    refs = [(i, None, 1, totals[i, scope]/area(projects[i])) for i in ids]
    lo, hi = (round(baseline["quant"](refs, q)*area(projects[row["project"]]), 2)
              for q in (alpha/2, 1-alpha/2))
    score, hit, width = run["metrics"]((lo, hi), row["truth"], alpha)
    groups[row["structure"], scope].append((score, hit, width))
    results.append([row["project"], row["structure"], scope, row["truth"], lo, hi, score, hit, width])
assert len(results) == 78
name = f"factory_equal_weight_{round(100*(1-alpha))}.csv"
with (Path(sys.argv[2])/name).open("w", encoding="utf8", newline="") as stream:
    writer = csv.writer(stream)
    writer.writerow(["project","structure","scope","truth","low","high","score","hit","width"])
    writer.writerows(results)
print("Equal weight macro score coverage width", *map(mean, zip(*(tuple(map(mean, zip(*g))) for g in groups.values()))))
