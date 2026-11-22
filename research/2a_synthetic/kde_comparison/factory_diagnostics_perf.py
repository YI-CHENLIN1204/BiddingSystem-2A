"""Time the research substring matcher in isolation; check paired missing/zero fixtures."""
import csv
import hashlib
import json
import platform
import statistics
import sys
import time
import tracemalloc
from pathlib import Path

from replay_factory_dictionary import normalize


def read(path):
    with path.open(encoding="utf-8-sig",newline="") as stream:
        return list(csv.DictReader(stream))


def main(src,dst,scale=False):
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
        for k in ((0,10,100,500) if scale else (0,10,100)):
            padding = [f"__synthetic_nonmatch_{i:04d}__" for i in range(max(0,k-len(order)))]
            assert not padding or all("__synthetic_nonmatch_" not in text for row in target for text in row)
            words=sorted(order[:k]+padding,key=lambda k:(-len(k),k))
            def match():
                return sum(any(w in name or w in spec for w in words) for name,spec in workload)
            hits=match()
            times=[]
            for _ in range(3):
                start=time.perf_counter()
                assert match()==hits
                times.append((time.perf_counter()-start)*1000)
            results.append(dict(rows=size,terms=k,hits=hits,ms=times,median_ms=statistics.median(times),p95_nearest_rank_ms=max(times)))
            if scale:
                tracemalloc.start()
                assert match()==hits
                _, peak = tracemalloc.get_traced_memory()
                tracemalloc.stop()
                results[-1].update(padded_terms=len(padding), lookup_peak_bytes=peak)
    filename = "factory_dictionary_scale.json" if scale else "factory_diagnostics_performance.json"
    (dst/filename).write_text(json.dumps(dict(python=sys.version,platform=platform.platform(),fixtures=checks,lookup=results),indent=2),encoding="utf8")


if __name__=="__main__":
    main(Path(sys.argv[1]),Path(sys.argv[2]),"--scale" in sys.argv[3:])

