"""Validate a quality-gated AB/BA crossover and report paired observed time."""
import argparse
import csv
import hashlib
import json
import math
import random
import re
from collections import defaultdict
from pathlib import Path
from statistics import mean, median

from intake import create_private_output

PHASES = ("setup", "mapping", "review", "error_detection", "error_correction", "maintenance")
SEED = 20261122

def analyze(source, destination):
    with source.open(encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    by_operator = defaultdict(list)
    for row in rows:
        operator = row["anonymous_operator_id"].strip()
        if not operator:
            raise ValueError("Missing operator key")
        by_operator[operator].append(row)
    pairs = []
    for operator, trials in sorted(by_operator.items()):
        if len(trials) != 2:
            raise ValueError(f"{operator}: expected two recorded trials")
        ordered = sorted(trials, key=lambda r: int(r["period"]))
        sequence = ordered[0]["randomized_sequence"]
        if sequence not in ("AB", "BA") or any(r["randomized_sequence"] != sequence for r in ordered):
            raise ValueError(f"{operator}: invalid sequence")
        expected = ["manual", "assisted"] if sequence == "AB" else ["assisted", "manual"]
        if [int(r["period"]) for r in ordered] != [1, 2] or [r["workflow_manual_or_assisted"] for r in ordered] != expected:
            raise ValueError(f"{operator}: incorrect order")
        if len({r["anonymous_boq_id"] for r in trials}) != 2 or len({r["boq_sha256"] for r in trials}) != 2:
            raise ValueError(f"{operator}: the same sheet cannot appear twice")
        if len({r["match_block_id"] for r in trials}) != 1 or not ordered[0]["match_block_id"].strip():
            raise ValueError(f"{operator}: record the matching block")
        totals, quality = {}, {}
        for r in trials:
            workflow = r["workflow_manual_or_assisted"]
            if not all(re.fullmatch(r"[0-9A-Fa-f]{64}", r[k]) for k in ("boq_sha256", "quality_reviewer_hash")):
                raise ValueError(f"{operator}: invalid source or reviewer hash")
            elapsed = [float(r[p + "_seconds"]) for p in PHASES]
            total = float(r["total_seconds"])
            if not all(math.isfinite(x) and x >= 0 for x in elapsed) or not math.isfinite(total) or total <= 0 or not math.isclose(sum(elapsed), total, abs_tol=1):
                raise ValueError(f"{operator}: inconsistent stage timing")
            n = int(r["item_count"])
            correct, false, unresolved = (int(r[k]) for k in ("accepted_correct_items", "false_auto_items", "unresolved_items"))
            if n <= 0 or not (0 <= correct <= n and 0 <= false <= n and 0 <= unresolved <= n and correct + unresolved <= n):
                raise ValueError(f"{operator}: invalid quality counts")
            totals[workflow] = total
            quality[workflow] = (correct == n and unresolved == 0, correct/n, false/n, unresolved/n)
        accepted = all(q[0] for q in quality.values())
        pairs.append(dict(operator=operator, sequence=sequence, quality_accepted=int(accepted),
                          manual_seconds=totals["manual"], assisted_seconds=totals["assisted"],
                          saved_seconds=totals["manual"]-totals["assisted"] if accepted else "",
                          manual_correct_rate=quality["manual"][1], assisted_correct_rate=quality["assisted"][1],
                          assisted_false_auto_rate=quality["assisted"][2],
                          assisted_unresolved_rate=quality["assisted"][3]))
    create_private_output(destination, parents=True)
    good = {s: [p["saved_seconds"] for p in pairs if p["quality_accepted"] and p["sequence"] == s] for s in ("AB", "BA")}
    result = dict(status="NOT RUN" if not pairs else "PARTIAL", operators=len(pairs),
                  quality_pass_pairs=sum(map(len, good.values())), sequence_counts={s:len(v) for s,v in good.items()},
                  quality_failed_pairs=sum(not p["quality_accepted"] for p in pairs),
                  source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(), seed=SEED,
                  median_by_sequence_seconds={s:median(v) if v else None for s,v in good.items()},
                  balanced_mean_seconds=None, bootstrap_ci95_seconds=None)
    if all(good.values()):
        result["balanced_mean_seconds"] = mean(mean(v) for v in good.values())
    if all(len(v) >= 2 for v in good.values()):
        rng = random.Random(SEED)
        estimates = sorted(mean(mean(rng.choices(v, k=len(v))) for v in good.values()) for _ in range(2000))
        result["bootstrap_ci95_seconds"] = [estimates[49], estimates[1949]]
    if pairs:
        with (destination / "paired.csv").open("w", encoding="utf-8", newline="") as stream:
            writer = csv.DictWriter(stream, pairs[0].keys())
            writer.writeheader()
            writer.writerows(pairs)
    (destination / "summary.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return result

if __name__ == "__main__":
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("observations", type=Path)
    cli.add_argument("new_output_directory", type=Path)
    args = cli.parse_args()
    print(json.dumps(analyze(args.observations, args.new_output_directory)))
