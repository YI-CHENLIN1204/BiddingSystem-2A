"""Compare weighted empirical and KDE intervals with identical A0 references."""
import csv
import math
import random
import runpy
import sys
from collections import defaultdict
from pathlib import Path
from statistics import mean

from replay_factory_b1 import features

root = Path(__file__).parent
baseline = runpy.run_path(str(root / "replay_factory_a0_b1.py"))
rows, projects, totals = (baseline[k] for k in ("rows", "p", "tot"))
for row in rows:
    row["truth"] = float(row["truth"])
grid = (.125, .25, .5, 1., 2.)


def interval(row, factor, alpha):
    ids = row["a_refs"].split("|")
    weights = list(map(float, row["a_weights"].split("|")))
    scope = row["scope"]
    values = [totals[i, scope] / features(projects[i])[0] for i in ids]
    area = features(projects[row["project"]])[0]
    width = max(values) - min(values)
    if not width:
        return None
    h = factor * width
    def cdf(x):
        return sum(w * math.erfc((v-x)/(math.sqrt(2)*h))/2 for v,w in zip(values,weights))/sum(weights)
    origin = cdf(0)
    def quantile(q):
        left, right = 0., max(values)+12*h
        target = origin+q*(1-origin)
        for _ in range(60):
            mid = (left+right)/2
            if cdf(mid)<target:
                left=mid
            else:
                right=mid
        return (left+right)/2*area
    return quantile(alpha/2), quantile(1-alpha/2)


def metrics(bounds, y, alpha):
    lo, hi = bounds
    return ((hi-lo+2/alpha*max(lo-y,y-hi,0))/y, int(lo<=y<=hi), (hi-lo)/y)


def macro(groups, fn):
    return mean(mean(fn(r) for r in g) for g in groups.values())


def groups_for(rs):
    groups = defaultdict(list)
    for r in rs:
        groups[r["structure"],r["scope"]].append(r)
    return groups


valid = groups_for(r for r in rows if r["split"]=="validation")
factor = min(grid,key=lambda f:macro(valid,lambda r: metrics(interval(r,f,.3),r["truth"],.3)[0]))
print("Validation bandwidth factor",factor)
alpha = float(sys.argv[3]) if len(sys.argv) > 3 else .3
test = [r for r in rows if r["split"]=="test"]
results = []
for r in test:
    bounds = interval(r,factor,alpha)
    assert bounds is not None and r["a_score"] is not None
    score,hit,width = metrics(bounds,r["truth"],alpha)
    ids = r["a_refs"].split("|")
    weights = list(map(float, r["a_weights"].split("|")))
    refs = [(i, None, w, totals[i,r["scope"]]/features(projects[i])[0]) for i,w in zip(ids,weights)]
    area = features(projects[r["project"]])[0]
    a0_bounds = (r["a_low"],r["a_high"]) if alpha == .3 else tuple(
        round(baseline["quant"](refs,q)*area,2) for q in (alpha/2,1-alpha/2))
    a0_score,a0_hit,a0_width = metrics(a0_bounds,r["truth"],alpha)
    results.append(dict(project=r["project"],scope=r["scope"],structure=r["structure"],truth=r["truth"],
                        refs=r["a_refs"],weights=r["a_weights"],factor=factor,
                        a0_low=a0_bounds[0],a0_high=a0_bounds[1],a1_low=bounds[0],a1_high=bounds[1],
                        a0_score=a0_score,a1_score=score,a0_hit=a0_hit,a1_hit=hit,
                        a0_width=a0_width,a1_width=width))
for scope in sorted({r["scope"] for r in results}):
    g=[r for r in results if r["scope"]==scope]
    print(scope,len(g),[(sum(r[m+"_hit"] for r in g),mean(r[m+"_score"] for r in g),mean(r[m+"_width"] for r in g)) for m in ("a0","a1")])
g=groups_for(results)
for field in ("score","hit","width"):
    print("macro",field,[macro(g,lambda r:r[m+"_"+field]) for m in ("a0","a1")])
by_structure=defaultdict(dict)
for r in results:
    by_structure[r["structure"]].setdefault(r["project"],[]).append(r)
rng=random.Random(20261122)
draws=[]
for _ in range(2000):
    sample=groups_for(r for cohort in by_structure.values()
                      for project in rng.choices(list(cohort.values()),k=len(cohort)) for r in project)
    draws.append(macro(sample,lambda r:r["a1_score"]-r["a0_score"]))
draws.sort()
print("Bootstrap 95%",draws[49],draws[1949])
filename = "factory_a0_a1_pairs.csv" if alpha == .3 else f"factory_a0_a1_{1-alpha:.0%}_pairs.csv".replace("%","pct")
with (Path(sys.argv[2])/filename).open("w",encoding="utf8",newline="") as f:
    writer=csv.DictWriter(f,fieldnames=results[0])
    writer.writeheader()
    writer.writerows(results)
