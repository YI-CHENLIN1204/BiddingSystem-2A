"""Replay the frozen descriptive factory reconstruction, offline."""
import csv
import hashlib
import json
import math
import sys
from collections import defaultdict
from functools import lru_cache
from itertools import combinations
from pathlib import Path
from statistics import mean

from probe_external_report import critical_bandwidth, distance, quantile


def features(p):
    return (float(p["total_floor_area"]), p["project_type"], p["structure_type"],
            int(p["floor_count"]), int(p["basement_floor_count"]), float(p["site_area"]),
            p["city"].replace("台", "臺"), int(p["start_date"][:4]))


def cluster(train, features_by_id, count):
    groups = [(i,) for i in sorted(train)]
    links = {}
    def key(a, b):
        return tuple(sorted((a, b)))
    for a, b in combinations(groups, 2):
        x, y = features_by_id[a[0]], features_by_id[b[0]]
        links[a, b] = (distance(x, y) + distance(y, x)) / 2
    while len(groups) > count:
        a, b = min(combinations(sorted(groups), 2), key=lambda ab: (links[ab], ab))
        groups.remove(a)
        groups.remove(b)
        merged = tuple(sorted(a + b))
        for c in groups:
            links[key(merged, c)] = (len(a)*links[key(a,c)] + len(b)*links[key(b,c)]) / len(merged)
        groups.append(merged)
    assert sorted(i for g in groups for i in g) == sorted(train)
    return groups


def main(source, output, past_only=False):
    freeze = json.loads(Path(__file__).with_name("factory_b1_freeze.json").read_text(encoding="utf8"))
    digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
    manifest = source / "manifest.json"
    assert digest(manifest) == freeze["source_manifest_sha256"]
    for name, checksum in json.loads(manifest.read_text(encoding="utf8"))["files_sha256"].items():
        assert digest(source / name) == checksum
    def rows(name):
        with (source / name).open(encoding="utf-8-sig", newline="") as f:
            yield from csv.DictReader(f)
    projects = {p["synthetic_project_id"]:p for p in rows("projects.csv")}
    f = {i:features(p) for i,p in projects.items()}
    train = {i for i,p in projects.items() if p["split"] == "train"}
    assert len(train) == 126 and len(projects) == 180
    groups = [] if past_only else cluster(train, f, freeze["groups"])

    @lru_cache(None)
    def historical_groups(cutoff):
        pool = {i for i in train if projects[i]["start_date"] < cutoff}
        return cluster(pool, f, freeze["groups"]) if len(pool) >= freeze["groups"] else []
    cost = ("structure_price",)
    totals = defaultdict(lambda:defaultdict(float))
    for r in rows("project_boq_rows.csv"):
        i = r["synthetic_project_id"]
        if r["major_category"] == "結構體工程":
            totals[i][cost] += float(r["amount"])
        if r["material_code"] in ("SYN-CONCRETE","SYN-REBAR") and r["price_scope"] == "MaterialOnly":
            scope = (r["material_code"], r["spec"], r["unit"], r["price_scope"])
            totals[i][scope] += float(r["quantity"])
    @lru_cache(None)
    def estimate(g, scope, axis):
        ids = [j for j in g if scope in totals[j] and f[j][axis] > 0]
        values = [totals[j][scope]/f[j][axis] for j in ids]
        assert all(math.isfinite(x) and x > 0 for x in values)
        h = critical_bandwidth(values) if len(ids) >= freeze["min_projects"] else None
        qs = {p:quantile(values,h,p) for p in (.05,.15,.5,.85,.95)} if h else {}
        return ids, qs
    records = []
    for i,p in sorted(projects.items()):
        if i in train or i == "FAC-20261122-SC-0052":
            continue
        candidate_groups = historical_groups(p["start_date"]) if past_only else groups
        g = min(candidate_groups, key=lambda g:(mean(distance(f[i], f[j]) for j in g),g), default=())
        assert i not in g
        assert not past_only or all(projects[j]["start_date"] < p["start_date"] for j in g)
        assert {s[0] for s in totals[i]} == {"SYN-CONCRETE","SYN-REBAR","structure_price"}
        for scope,truth in totals[i].items():
            assert truth > 0 and math.isfinite(truth)
            for method,axis in ([("B1",5),("B2",0)] if scope == cost else [("B1",0)]):
                ids, qs = estimate(g, scope, axis)
                for level,lower,upper in ((.7,.15,.85),(.9,.05,.95)):
                    row = dict(project=i,split=p["split"],structure=p["structure_type"],method=method,
                               scope=scope[0],spec="|".join(scope[1:]),level=level,truth=truth,
                               refs="|".join(ids),n=len(ids),later_refs=sum(projects[j]["start_date"]>p["start_date"] for j in ids),
                               status="ok" if qs else ("below_min" if len(ids)<5 else "zero_span"),
                               low=None,median=None,high=None,covered=None,relative_score=None)
                    if qs:
                        lo,med,hi = [qs[q]*f[i][axis] for q in (lower,.5,upper)]
                        row.update(low=lo,median=med,high=hi,covered=int(lo<=truth<=hi),
                                   relative_score=(hi-lo+2/(1-level)*max(lo-truth,truth-hi,0))/truth)
                    records.append(row)
    output.mkdir(parents=True,exist_ok=True)
    with (output/"factory_b1_pairs.csv").open("w",encoding="utf8",newline="") as out:
        w=csv.DictWriter(out,fieldnames=records[0])
        w.writeheader()
        w.writerows(records)
    summary=[]
    for split,method,scope,level in sorted({(r["split"],r["method"],r["scope"],r["level"]) for r in records}):
        population=[r for r in records if (r["split"],r["method"],r["scope"],r["level"])==(split,method,scope,level)]
        usable=[r for r in population if r["status"]=="ok"]
        summary.append(dict(split=split,method=method,scope=scope,level=level,n=len(population),
                            estimated=len(usable),covered=sum(r["covered"] for r in usable),
                            relative_score=mean(r["relative_score"] for r in usable) if usable else None))
    result=dict(group_sizes=sorted(map(len,groups)),groups=sorted(groups),pairs_sha256=digest(output/"factory_b1_pairs.csv"),
                summary=summary)
    (output/"factory_b1_summary.json").write_text(json.dumps(result,indent=2),encoding="utf8")
    print(json.dumps(summary))


if __name__ == "__main__":
    main(Path(sys.argv[1]),Path(sys.argv[2]),"--past-only" in sys.argv[3:])

