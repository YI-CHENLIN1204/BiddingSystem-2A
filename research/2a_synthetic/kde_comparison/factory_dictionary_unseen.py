import json
import sys
from collections import Counter, defaultdict
from pathlib import Path
from factory_diagnostics_perf import read
from replay_factory_dictionary import normalize
import hashlib

source, output = map(Path, sys.argv[1:])
path = source / "project_boq_rows.csv"
manifest = source / "manifest.json"
assert hashlib.sha256(manifest.read_bytes()).hexdigest() == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
assert hashlib.sha256(path.read_bytes()).hexdigest() == json.loads(manifest.read_text(encoding="utf8"))["files_sha256"][path.name]
rows = read(path)
train = [r for r in rows if r["split"] == "train"]
chapters = defaultdict(set)
for row in train:
    chapters[row["major_category"]].add(row["material_code"])
held = {min(codes) for codes in chapters.values() if len(codes) >= 2}
removed = {normalize(r["item_name"]) for r in train if r["material_code"] in held}
terms = defaultdict(set)
for row in train:
    name = normalize(row["item_name"])
    if name not in removed:
        terms[name].add(row["material_code"])
keys = sorted(terms, key=lambda term: (-len(term), term))
results = {}
for split in ("validation", "test"):
    exact, contains = Counter(), Counter()
    for row in rows:
        if row["split"] != split or row["synthetic_project_id"] == "FAC-20261122-SC-0052":
            continue
        name = normalize(row["item_name"])
        truth = row["material_code"]
        if truth not in held or name in terms:
            continue
        spec = normalize(row["spec"])
        match = next((key for key in keys if key in name or key in spec), None)
        candidates = terms[match] if match is not None else set()
        exact["pending"] += 1
        contains["ambiguous" if len(candidates)>1 else "pending" if not candidates
                 else "correct" if truth in candidates else "wrong"] += 1
    assert exact.total() == contains.total() == {"validation":850,"test":781}[split]
    results[split] = {"exact":dict(exact),"contains":dict(contains)}
output.write_text(json.dumps(dict(held=sorted(held),terms=len(keys),results=results),indent=2),encoding="utf8")
print(results)
