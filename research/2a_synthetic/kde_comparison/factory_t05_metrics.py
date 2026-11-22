"""Report planned T05 diagnostics for the existing 70% factory replay."""
import csv
from collections import defaultdict
from pathlib import Path
import runpy
from statistics import mean
import sys

run = runpy.run_path(str(Path(__file__).with_name("replay_factory_a1.py")))
assert run["alpha"] == .3 and not run["past_only"]
groups = defaultdict(list)
for row in run["test"]:
    median = run["interval"](row, run["factor"], 1.)[0]
    weights = list(map(float, row["a_weights"].split("|")))
    truth = row["truth"]
    groups[row["structure"], row["scope"]].append((
        abs(row["a_median"]-truth)/truth, abs(median-truth)/truth,
        sum(weights)**2/sum(weight**2 for weight in weights)))
assert sum(map(len, groups.values())) == 78
with (Path(sys.argv[2])/"factory_t05_metrics.csv").open("w", encoding="utf8", newline="") as stream:
    writer = csv.writer(stream)
    writer.writerow(["structure","scope","n","a0_p50_mape","a1_p50_mape","mean_ess"])
    for (structure, scope), group in sorted(groups.items()):
        writer.writerow([structure, scope, len(group), *map(mean, zip(*group))])
print("Macro", *map(mean, zip(*(tuple(map(mean,zip(*g))) for g in groups.values()))))
