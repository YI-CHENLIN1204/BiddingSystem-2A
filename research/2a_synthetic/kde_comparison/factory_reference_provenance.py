"""Check reference provenance in frozen factory output."""
import csv
import hashlib
import json
import sys
from pathlib import Path

source, pairs, results = map(Path, sys.argv[1:])
def read(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))

manifest = (source/"manifest.json").read_bytes()
assert hashlib.sha256(manifest).hexdigest() == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
path = source/"projects.csv"
assert hashlib.sha256(path.read_bytes()).hexdigest() == json.loads(manifest)["files_sha256"][path.name]
data = read(path)
projects = {r["synthetic_project_id"]: r for r in data}
assert len(data) == len(projects) == 180
for path, field, past in (
    (pairs, "a_refs", False),
    (results/"factory_b1_pairs.csv", "refs", False),
    (results/"past_only_factory_a0_a1_pairs.csv", "refs", True),
    (results/"past_only_factory_a0_a1_90pct_pairs.csv", "refs", True),
):
    rows = read(path)
    checks = later = 0
    for row in rows:
        target = projects[row["project"]]
        assert target["split"] in ("validation","test")
        assert target["split"] == row.get("split","test")
        assert row["project"] != "FAC-20261122-SC-0052"
        refs = list(filter(None,row[field].split("|")))
        assert len(refs)==len(set(refs))
        for ref in refs:
            history = projects[ref]
            assert ref!=row["project"] and history["split"]=="train"
            nonpast = history["start_date"] >= target["start_date"]
            assert not past or not nonpast
            later += nonpast
            checks += 1
    print(path.name, len(rows), checks, later)
