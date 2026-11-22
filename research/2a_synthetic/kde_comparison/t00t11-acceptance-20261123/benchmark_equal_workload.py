"""Benchmark the frozen E2 K5 prediction stage, excluding candidate selection."""
import argparse
import csv
import hashlib
import json
import math
import platform
from collections import defaultdict
from datetime import date, datetime, timezone
from pathlib import Path
from statistics import median
from time import perf_counter_ns

from probe_external_report import critical_bandwidth, quantile


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def empirical(samples, q):
    ordered = sorted(samples)
    cutoff = q * sum(w for _, w in samples)
    total = 0
    for value, weight in ordered:
        total += weight
        if total >= cutoff:
            return value
    return ordered[-1][0]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("pairs", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    manifest = args.source / "manifest.json"
    assert digest(manifest) == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
    for name, sha in json.loads(manifest.read_text(encoding="utf8"))["files_sha256"].items():
        assert digest(args.source / name) == sha
    projects = {p["synthetic_project_id"]: p for p in rows(args.source / "projects.csv")}
    totals = defaultdict(float)
    for row in rows(args.source / "project_boq_rows.csv"):
        pid = row["synthetic_project_id"]
        if row["major_category"] == "結構體工程":
            totals[pid, ("structure_price", "", "TWD", "chapter_total")] += float(row["amount"])
        if row["material_code"] in ("SYN-CONCRETE", "SYN-REBAR") and row["price_scope"] == "MaterialOnly":
            scope = tuple(row[k] for k in ("material_code", "spec", "unit", "price_scope"))
            totals[pid, scope] += float(row["quantity"])

    def area(project):
        return float(project["total_floor_area"])

    def weight(target, ref):
        def rel(k):
            a, b = float(target[k]), float(ref[k])
            return max(0, 1 - abs(a - b) / a)
        d = lambda k: abs(int(target[k]) - int(ref[k]))
        year = abs(int(target["start_date"][:4]) - int(ref["start_date"][:4]))
        score = round(25*(target["structure_type"] == ref["structure_type"])
                      + 30*rel("total_floor_area") + 10*rel("site_area")
                      + 15*max(0, 1-.1*d("floor_count")-.15*d("basement_floor_count"))
                      + 10*max(0, 1-year/20) + 10*(target["city"] == ref["city"]), 2)
        days = (date.fromisoformat(target["start_date"]) - date.fromisoformat(ref["start_date"])).days
        assert days > 0 and ref["split"] == "train"
        return score / 100 * .5 ** (days / 365.2425 / 3)

    paired = defaultdict(dict)
    for row in rows(args.pairs):
        if (row["split"], row["k"], row["level"], row["status"]) == ("test", "5", "0.7", "ok"):
            scope = tuple(row[k] for k in ("scope", "spec", "unit", "work_scope"))
            paired[row["project"], scope][row["method"]] = row

    methods = ("A0-K", "B1-K")
    batches = {method: [] for method in methods}
    scopes = defaultdict(int)
    for (pid, scope), pair in sorted(paired.items()):
        if set(pair) != set(methods):
            continue
        scopes[scope[0]] += 1
        for method in methods:
            row = pair[method]
            refs = row["refs"].split("|")
            assert len(set(refs)) == 5
            values = [totals[ref, scope] / area(projects[ref]) for ref in refs]
            assert all(value > 0 for value in values)
            weights = [weight(projects[pid], projects[ref]) for ref in refs]
            expected = tuple(float(row[k]) for k in ("low", "median", "high"))
            batches[method].append((values, weights, area(projects[pid]), expected))
    assert sum(scopes.values()) == 30

    def workload(method):
        result = []
        for values, weights, target_area, _ in batches[method]:
            if method == "A0-K":
                samples = list(zip(values, weights))
                estimate = lambda p: empirical(samples, p)
            else:
                h = critical_bandwidth(values)
                assert h is not None
                estimate = lambda p: quantile(values, h, p)
            result.append(tuple(target_area * estimate(p) for p in (.15, .5, .85)))
        return result

    expected = {method: workload(method) for method in methods}
    for method in methods:
        for actual, (_, _, _, frozen) in zip(expected[method], batches[method], strict=True):
            assert all(math.isclose(a, b, rel_tol=1e-9) for a, b in zip(actual, frozen, strict=True))

    times = {method: [] for method in methods}
    for i in range(10):
        for method in methods[::(1 if i % 2 == 0 else -1)]:
            start = perf_counter_ns()
            result = workload(method)
            times[method].append((perf_counter_ns() - start) / 1e6)
            assert result == expected[method]
    answer = dict(utc=datetime.now(timezone.utc).isoformat(), runtime=platform.python_version(),
                  platform=platform.platform(), processor=platform.processor(),
                  pairs_sha256=digest(args.pairs), source_manifest_sha256=digest(manifest),
                  program_sha256=digest(Path(__file__)),
                  kernel_sha256=digest(Path(__file__).with_name("probe_external_report.py")),
                  scope_counts=scopes, trials=10, references_per_case_item=5, warmup_batches=1,
                  scope="in-process prediction only; excludes selection, IO, API and DB",
                  paired_outputs_verified=60,
                  methods={m: dict(batch_ms=t, p50_ms=median(t), p95_ms=sorted(t)[math.ceil(.95*len(t))-1])
                           for m, t in times.items()})
    with args.output.open("x", encoding="utf8") as stream:
        json.dump(answer, stream, indent=2)
    print(json.dumps(answer, indent=2))


if __name__ == "__main__":
    main()
