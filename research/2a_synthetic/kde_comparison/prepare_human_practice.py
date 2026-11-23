"""Build a matched, synthetic-only T03 practice packet with separate answers."""
import argparse
import csv
import json
from collections import Counter, defaultdict
from itertools import combinations
from pathlib import Path

from audit_frozen_factory import read, sha, write

def main(source, output):
    manifest = source / "manifest.json"
    assert sha(manifest) == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
    declared = json.loads(manifest.read_text(encoding="utf-8"))["files_sha256"]
    assert sha(source / "project_boq_rows.csv") == declared["project_boq_rows.csv"]
    projects = defaultdict(list)
    for row in read(source / "project_boq_rows.csv"):
        if row["split"] == "test" and "SC-0052" not in row["synthetic_project_id"]:
            projects[row["synthetic_project_id"]].append(row)
    def fingerprint(row):
        return tuple(row[k].strip() for k in ("item_name","spec","unit"))
    for project, rows in projects.items():
        unique = {}
        for row in rows:
            unique.setdefault(fingerprint(row), row)
        projects[project] = list(unique.values())
    def structure(project):
        return projects[project][0]["structure_type"]
    def match(a, b):
        left, right = projects[a], projects[b]
        distribution = lambda rows: Counter(r["major_category"] for r in rows)
        l,r = distribution(left),distribution(right)
        return (abs(len(left)-len(right)), sum(abs(l[k]-r[k]) for k in l.keys() | r.keys()), a,b)
    _, _, a, b = min(match(a,b) for a,b in combinations(sorted(projects),2) if structure(a)==structure(b))
    output.mkdir(parents=True, exist_ok=False)
    sheets, answers = [], []
    for label, project in zip(("X","Y"),(a,b),strict=True):
        rows = projects[project]
        assert all(r["data_origin"]=="SYNTHETIC" for r in rows)
        public = ("item_name","major_category","spec","unit","quantity","unit_price","amount")
        path = output/f"practice-{label}.csv"
        with path.open("w", encoding="utf-8-sig", newline="") as stream:
            writer = csv.DictWriter(stream, ("row", *public))
            writer.writeheader()
            for index,r in enumerate(rows,1):
                writer.writerow(dict(row=index, **{k:r[k] for k in public}))
                answers.append(dict(sheet=label,row=index,synthetic_material_code=r["material_code"]))
        sheets.append(dict(sheet=label,project=project,structure=structure(project),rows=len(rows),
                           materials=len({r["material_code"] for r in rows}),
                           by_category=dict(Counter(r["major_category"] for r in rows)),sha256=sha(path)))
    write(output/"practice-reviewer-key.csv",answers)
    write(output/"assignment-template.csv",[
        dict(sequence=s,period=i,sheet=f,workflow=m)
        for s,order in (("AB",("manual","assisted")),("BA",("assisted","manual")))
        for sheets_order in (("X","Y"),("Y","X"))
        for i,(f,m) in enumerate(zip(sheets_order,order,strict=True),1)])
    result = dict(status="SYNTHETIC PRACTICE ONLY", source_sha256=sha(manifest),sheets=sheets,
                  reviewer_key_sha256=sha(output/"practice-reviewer-key.csv"),
                  assignment_template_sha256=sha(output/"assignment-template.csv"))
    (output/"manifest.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
    print(json.dumps([{k:r[k] for k in ("sheet","project","structure","rows","materials")} for r in sheets]))

if __name__ == "__main__":
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument("source",type=Path)
    cli.add_argument("new_output",type=Path)
    args=cli.parse_args()
    main(args.source,args.new_output)
