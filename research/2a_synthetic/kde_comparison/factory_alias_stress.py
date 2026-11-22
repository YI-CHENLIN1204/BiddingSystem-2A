import csv
import hashlib
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

from factory_diagnostics_perf import read
from replay_factory_dictionary import normalize

source, output = map(Path, sys.argv[1:])
manifest = (source / "manifest.json").read_bytes()
assert hashlib.sha256(manifest).hexdigest() == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
path = source / "project_boq_rows.csv"
assert hashlib.sha256(path.read_bytes()).hexdigest() == json.loads(manifest)["files_sha256"][path.name]
rows = read(path)
terms = defaultdict(set)
for row in rows:
    if row["split"] == "train":
        terms[normalize(row["item_name"])].add(row["material_code"])
keys = sorted(terms, key=lambda key: (-len(key), key))
aliases = {"SYN-CONCRETE": "預拌砼", "SYN-REBAR": "鋼質補強筋"}
results = []
for row in rows:
    code = row["material_code"]
    if row["split"] not in ("validation", "test") or code not in aliases:
        continue
    if row["synthetic_project_id"] == "FAC-20261122-SC-0052":
        continue
    original = normalize(row["item_name"])
    assert terms[original] == {code}
    for variant, name in (("control", original), ("prefix", "工程用 " + original),
                          ("alias", normalize(aliases[code]))):
        if variant != "control":
            assert name not in terms
        spec = normalize(row["spec"])
        match = next((key for key in keys if key in name or key in spec), None)
        for method, candidates in (("exact", terms.get(name, set())), ("contains", terms.get(match, set()))):
            status = ("pending" if not candidates else "ambiguous" if len(candidates)>1
                      else "correct" if code in candidates else "wrong")
            results.append(dict(split=row["split"], project=row["synthetic_project_id"],
                                line=row["synthetic_line_id"], code=code, variant=variant,
                                method=method, status=status))
with output.open("w", encoding="utf8", newline="") as stream:
    writer = csv.DictWriter(stream, fieldnames=results[0])
    writer.writeheader()
    writer.writerows(results)
print(sorted(Counter((r["split"],r["variant"],r["method"],r["status"]) for r in results).items()))
