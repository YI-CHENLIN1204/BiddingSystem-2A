"""Compare time-filtered A0 and B1 on identical case-scope targets."""
import csv
import sys
from pathlib import Path
from statistics import mean

b1_path, a0_dir = map(Path, sys.argv[1:])
def read(path):
    with path.open(encoding="utf8", newline="") as stream:
        return list(csv.DictReader(stream))

b1 = read(b1_path)
print("level,scope,total,a0_available,b1_available,paired,a0_score,b1_score")
for level, suffix in (("0.7",""), ("0.9","_90pct")):
    a0 = {(r["project"],r["scope"]):r for r in read(
        a0_dir / f"past_only_factory_a0_a1{suffix}_pairs.csv")}
    for scope in ("SYN-CONCRETE","SYN-REBAR","structure_price"):
        cohort = [r for r in b1 if r["method"]=="B1" and r["split"]=="test"
                  and r["level"]==level and r["scope"]==scope]
        assert len(cohort) == len({r["project"] for r in cohort}) == 26
        usable = [r for r in cohort if r["status"]=="ok"]
        paired = [(a0[r["project"],scope],r) for r in usable if (r["project"],scope) in a0]
        assert all(float(a["truth"])==float(b["truth"]) for a,b in paired)
        scores = [f"{mean(float(pair[i][field]) for pair in paired):.6f}" if paired else ""
                  for i,field in ((0,"a0_score"),(1,"relative_score"))]
        print(level,scope,26,sum(s==scope for _,s in a0),len(usable),len(paired),*scores,sep=",")
