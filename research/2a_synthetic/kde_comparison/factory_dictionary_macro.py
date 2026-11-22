"""Summarize the dictionary replay with projects as the resampling unit."""
import csv
import random
import sys
from pathlib import Path
from collections import Counter, defaultdict
from statistics import mean

counts = defaultdict(Counter)
with open(sys.argv[1], encoding="utf-8-sig", newline="") as stream:
    for row in csv.DictReader(stream):
        counts[row["split"], int(row["k"]), row["project"]][row["status"]] += 1

with open(sys.argv[2], "w", encoding="utf8", newline="") as stream:
    writer = csv.writer(stream)
    writer.writerow(["split","k","projects","macro_correct","ci95_low","ci95_high"])
    for split, k in sorted({key[:2] for key in counts}):
        rates = [count["correct"]/count.total() for key,count in sorted(counts.items())
                 if key[:2] == (split,k)]
        assert len(rates) == {"validation":27, "test":26}[split]
        rng = random.Random(20261122)
        draws = sorted(mean(rng.choices(rates,k=len(rates))) for _ in range(2000))
        writer.writerow([split,k,len(rates),mean(rates),draws[49],draws[1949]])

with Path(sys.argv[2]).with_name("dictionary_paired.csv").open("w", encoding="utf8", newline="") as stream:
    writer = csv.writer(stream)
    writer.writerow(["split","from_k","to_k","projects","mean_gain","ci95_low","ci95_high","improved","tied"])
    for split in ("validation", "test"):
        ids = sorted(project for cohort,k,project in counts if cohort == split and k == 50)
        for low,high in ((50,67),(67,100),(50,100)):
            gains = []
            for project in ids:
                a,b = counts[split,low,project], counts[split,high,project]
                assert a.total() == b.total() > 0 and b["correct"] >= a["correct"]
                gains.append((b["correct"]-a["correct"])/a.total())
            rng = random.Random(20261122)
            draws = sorted(mean(rng.choices(gains,k=len(gains))) for _ in range(2000))
            writer.writerow([split,low,high,len(gains),mean(gains),draws[49],draws[1949],
                             sum(gain>0 for gain in gains),gains.count(0)])

