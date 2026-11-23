"""Stratify synthetic dictionary replay by project origin scenario and structure."""
import argparse
from collections import Counter, defaultdict
from pathlib import Path
from audit_frozen_factory import read, write, sha

def main(source, mapping, out):
    projects = {r["synthetic_project_id"]:r for r in read(source/"projects.csv")}
    rows = read(mapping)
    strata = defaultdict(Counter)
    for r in rows:
        p = projects[r["project"]]
        assert p["split"] == r["split"] and p["split"] != "train"
        strata[r["k"],r["split"],p["structure_type"],p["factory_scenario"]][r["status"]] += 1
    output = []
    for (k, split, structure, scenario), counts in sorted(strata.items(), key=lambda item: (int(item[0][0]), *item[0][1:])):
        n = counts.total()
        correct = counts["correct"]
        wrong = counts["wrong"]
        output.append(dict(k=k,split=split,structure=structure,synthetic_scenario=scenario,n=n,
            correct=correct,wrong=wrong,pending=n-correct-wrong,correct_auto_rate=correct/n,
            false_auto_rate=wrong/n, auto_precision=None if correct+wrong==0 else correct/(correct+wrong),
            source_projects_sha256=sha(source/"projects.csv"), mapping_sha256=sha(mapping)))
    assert sum(r["n"] for r in output) == len(rows)
    write(out, output)
    print(f"{len(rows)} rows, {len(output)} stratum results")

if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ("source","mapping","output"):
        parser.add_argument(name,type=Path)
    a=parser.parse_args()
    main(a.source,a.mapping,a.output)
