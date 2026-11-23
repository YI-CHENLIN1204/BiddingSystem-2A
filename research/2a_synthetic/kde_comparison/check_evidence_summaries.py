"""Reconcile frozen summaries to their row-level inputs without tuning models."""
import csv
import json
import math
import sys
from collections import Counter, defaultdict
from pathlib import Path

root = Path(sys.argv[1])
def read(name):
    with (root/name).open(encoding="utf-8-sig",newline="") as stream:
        return list(csv.DictReader(stream))

p = {r["synthetic_project_id"]:r for r in read("source/projects.csv")}
curves, strata = defaultdict(Counter), defaultdict(Counter)
for r in read("results/dictionary/mapping_per_item.csv"):
    case=p[r["project"]]
    assert r["split"]==case["split"]
    curves[r["k"],r["split"]][r["status"]]+=1
    strata[r["k"],r["split"],case["structure_type"],case["factory_scenario"]][r["status"]]+=1
for name, group, fields in [
    ("dictionary_growth.csv",curves,("k","split")),
    ("scenario-strata.csv",strata,("k","split","structure","synthetic_scenario"))]:
    published=read("results/dictionary/"+name)
    assert {tuple(r[k] for k in fields) for r in published}==set(group)
    for r in published:
        c=group[tuple(r[k] for k in fields)]
        assert c.total()==int(r.get("n",r.get("total")))
        assert all(c[s]==int(r[s]) for s in ("correct","wrong","pending"))

mapping=defaultdict(list)
for r in read("results/mapping/dictionary_material_per_case.csv"):
    if r["top1"]:
        assert int(r["top1"])<=int(r["top3"])
    mapping[r["split"],r["variant"],r["method"]].append(r)
for r in read("results/mapping/dictionary_material_summary.csv"):
    matches=mapping[r["split"],r["variant"],r["method"]]
    assert len(matches)==int(r["n"])
    for k in ("correct","wrong","pending","safe_release","unsafe_release","unknown","conflict","top1","top3"):
        if r[k]:
            assert int(r[k])==sum(int(m[k] or 0) for m in matches),(k,r)

clusters=read("results/cluster-count/per-case.csv")
good={(r["scope"],r["level"],r["groups"],r["project"]): float(r["score"])
      for r in clusters if r["status"]=="ok"}
overview=read("results/cluster-count/summary.csv")
for r in overview:
    scope,level,group=(r["scope"],r["level"],r["groups"])
    pool={p for s,l,g,p in good if (s,l,g)==(scope,level,group)}
    shared={p for p in pool if all((scope,level,g,p) in good for g in ("3","4","5"))}
    assert len(pool)==int(r["estimated"]) and len(shared)==int(r["common_estimated"])
    if shared:
        score=math.fsum(good[scope,level,group,p] for p in shared)/len(shared)
        assert math.isclose(score,float(r["common_score"]),abs_tol=1e-12)

truth=read("source/diagnostic_truth.csv")
for r in truth:
    assert r["requires_data_review"]=="True" and r["history_eligible"]=="False"
    assert not r["engineering_anomaly"]
print(json.dumps(dict(dictionary_curves=len(curves),dictionary_strata=len(strata),
    candidate_summary_groups=len(mapping),cluster_summary_cells=len(overview),
    synthetic_data_quality_labels=len(truth),status="PASS"),indent=2))
