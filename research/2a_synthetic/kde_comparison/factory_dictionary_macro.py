"""Summarize the dictionary replay with projects as the resampling unit."""
import csv
import random
import sys
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
