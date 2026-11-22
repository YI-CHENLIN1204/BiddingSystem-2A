"""Time the research substring matcher in isolation; check paired missing/zero fixtures."""
import csv
import hashlib
import json
import platform
import statistics
import sys
import time
from pathlib import Path

from replay_factory_dictionary import normalize


def read(path):
    with path.open(encoding="utf-8-sig",newline="") as stream:
        return list(csv.DictReader(stream))


def main(src,dst):
    manifest=src/"manifest.json"
    assert hashlib.sha256(manifest.read_bytes()).hexdigest()=="e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
    for name,expected in json.loads(manifest.read_text(encoding="utf8"))["files_sha256"].items():
        assert hashlib.sha256((src/name).read_bytes()).hexdigest()==expected
    rows=read(src/"project_boq_rows.csv")
    diag=read(src/"diagnostic_boq_rows.csv")
    truth=read(src/"diagnostic_truth.csv")
    checks=[]
    for t in truth:
        matching=[r for r in diag if r["synthetic_project_id"]==t["diagnostic_project_id"] and r["material_code"]==t["material_code"]]
        state="MissingRequiredItem" if not matching else "ExplicitRecordedZero" if all(float(r["quantity"])==0 for r in matching) else "present"
        assert state==t["status"] and not t["engineering_anomaly"].strip()
        checks.append([t["diagnostic_project_id"],state,len(matching)])
    train=[r for r in rows if r["split"]=="train"]
    terms={}
    for r in train:
        terms.setdefault(normalize(r["item_name"]),set()).add(r["synthetic_project_id"])
    order=sorted(terms,key=lambda k:(-len(terms[k]),k))
    target=[(normalize(r["item_name"]),normalize(r["spec"])) for r in rows if r["split"]=="validation"]
    results=[]
    for size in (1000,10000,50000):
        workload=[target[i%len(target)] for i in range(size)]
        for k in (0,10,100):
            words=sorted(order[:k],key=lambda k:(-len(k),k))
            def match():
                return sum(any(w in name or w in spec for w in words) for name,spec in workload)
            hits=match()
            times=[]
            for _ in range(3):
                start=time.perf_counter()
                assert match()==hits
                times.append((time.perf_counter()-start)*1000)
            results.append(dict(rows=size,terms=k,hits=hits,ms=times,median_ms=statistics.median(times),p95_nearest_rank_ms=max(times)))
    (dst/"factory_diagnostics_performance.json").write_text(json.dumps(dict(python=sys.version,platform=platform.platform(),fixtures=checks,lookup=results),indent=2),encoding="utf8")


if __name__=="__main__":
    main(Path(sys.argv[1]),Path(sys.argv[2]))
