"""Check sensitivity of the frozen A0/A1 score comparison to each test project."""
import csv
import sys
from collections import Counter, defaultdict
from statistics import mean

with open(sys.argv[1], encoding="utf-8-sig", newline="") as stream:
    rows = list(csv.DictReader(stream))
projects = sorted({r["project"] for r in rows})
assert len(projects) == 26 and len(rows) == 78
assert set(Counter(r["project"] for r in rows).values()) == {3}

def improvement(excluded=None):
    groups = defaultdict(list)
    for row in rows:
        if row["project"] != excluded:
            groups[row["structure"], row["scope"]].append(row)
    assert len(groups) == 9
    scores = [mean(mean(float(r[m+"_score"]) for r in group)
                   for group in groups.values()) for m in ("a0", "a1")]
    return 1 - scores[1]/scores[0]

writer = csv.writer(sys.stdout)
writer.writerow(["excluded_project", "relative_improvement"])
writer.writerow(["none", improvement()])
for project in projects:
    writer.writerow([project, improvement(project)])
