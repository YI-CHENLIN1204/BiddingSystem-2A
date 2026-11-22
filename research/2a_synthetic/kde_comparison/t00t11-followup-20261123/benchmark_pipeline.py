"""Measure the frozen E2 K5 offline pipeline on common estimable case-items."""
import csv
import ctypes
import hashlib
import json
import math
import platform
import sys
from collections import defaultdict
from datetime import datetime, timezone
from statistics import median
from pathlib import Path
from time import perf_counter_ns

replay, source, pairs, output = map(Path, sys.argv[1:])
digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
sys.path.insert(0, str(replay.resolve().parent))
start = perf_counter_ns()
args = sys.argv
sys.argv = [str(replay), str(source), str(output)]
env = {"__file__": str(replay)}
prefix, marker, _ = replay.read_text(encoding="utf8").partition("freeze = dict(")
assert marker
exec(compile(prefix, str(replay), "exec"), env)
sys.argv = args
load_ms = (perf_counter_ns() - start) / 1e6
projects, train, dates, days, f, totals = (env[k] for k in
    ("projects", "train", "dates", "days", "f", "totals"))
similarity, empirical, distance, groups, bandwidth, quantile = (env[k] for k in
    ("similarity", "empirical", "distance", "groups", "critical_bandwidth", "quantile"))
workload = defaultdict(dict)
with pairs.open(encoding="utf-8-sig", newline="") as fp:
    for row in csv.DictReader(fp):
        if (row["split"], row["k"], row["level"], row["status"]) == ("test", "5", "0.7", "ok"):
            scope = tuple(row[k] for k in ("scope", "spec", "unit", "work_scope"))
            workload[row["project"], scope][row["method"]] = row
methods = ("A0-K", "B1-K")
tasks = sorted((key, pair) for key, pair in workload.items() if set(pair) == set(methods))
assert len(tasks) == 30


def milliseconds(start_ns):
    return (perf_counter_ns() - start_ns) / 1e6


records = []
batches = []
for trial in range(3):
    for method in (methods if trial % 2 == 0 else methods[::-1]):
        groups.cache_clear()
        batch_start = perf_counter_ns()
        for (i, scope), pair in tasks:
            total_start = phase_start = perf_counter_ns()
            pool = tuple(sorted(j for j in train if dates[j] < dates[i]))
            pool_ms = milliseconds(phase_start)
            groups_ms = bandwidth_ms = 0.0
            if method == "B1-K":
                phase_start = perf_counter_ns()
                partition = groups(pool)
                groups_ms = milliseconds(phase_start)
                phase_start = perf_counter_ns()
                nearest = min(partition, key=lambda g:(sum(distance(f[i], f[j]) for j in g)/len(g),g))
                refs = sorted((j for j in nearest if totals[j].get(scope, 0) > 0),
                              key=lambda j: (distance(f[i], f[j]), j))[:5]
            else:
                phase_start = perf_counter_ns()
                scores = {j:similarity(i,j) for j in pool if f[j][1] == f[i][1]
                          and (f[i][2] != "SRC" or f[j][2] == "SRC") and totals[j].get(scope,0)>0}
                weights = {j:s/100*.5**(abs((days[i]-days[j]).days)/365.2425/3)
                           for j,s in scores.items() if s > 0}
                refs = sorted(weights, key=lambda j:(-scores[j],-weights[j],j))[:5]
            selection_ms = milliseconds(phase_start)
            assert "|".join(refs) == pair[method]["refs"] and len(refs) == 5
            phase_start = perf_counter_ns()
            xs = [totals[j][scope]/f[j][0] for j in refs]
            preparation_ms = milliseconds(phase_start)
            if method == "B1-K":
                phase_start = perf_counter_ns()
                h = bandwidth(xs)
                assert h is not None
                bandwidth_ms = milliseconds(phase_start)
                evaluate = lambda q: quantile(xs, h, q)
            else:
                samples = list(zip(xs, (weights[j] for j in refs)))
                evaluate = lambda q: empirical(samples, q)
            phase_start = perf_counter_ns()
            values = [evaluate(q)*f[i][0] for q in (.15,.5,.85)]
            quantile_ms = milliseconds(phase_start)
            total_ms = milliseconds(total_start)
            assert all(math.isclose(v,float(pair[method][k]),rel_tol=1e-9)
                       for v,k in zip(values,("low","median","high"),strict=True))
            records.append(dict(trial=trial, method=method, case=i, scope=scope, pool_ms=pool_ms,
                                cluster_ms=groups_ms, select_ms=selection_ms, prepare_ms=preparation_ms,
                                bandwidth_ms=bandwidth_ms, quantile_ms=quantile_ms, total_ms=total_ms))
        batches.append(dict(trial=trial, method=method, ms=milliseconds(batch_start)))


def stats(xs):
    return dict(p50=median(xs), p95=sorted(xs)[math.ceil(.95*len(xs))-1], n=len(xs))


def windows_peak_rss():
    if sys.platform != "win32":
        return None
    size = ctypes.c_size_t
    dword = ctypes.c_uint32
    class Counters(ctypes.Structure):
        _fields_ = [("cb", dword), ("faults", dword), ("peak", size)] + [
            (f"extra_{i}", size) for i in range(7)]
    current = ctypes.windll.kernel32.GetCurrentProcess
    current.restype = ctypes.c_void_p
    inspect = ctypes.windll.psapi.GetProcessMemoryInfo
    inspect.argtypes = (ctypes.c_void_p, ctypes.POINTER(Counters), dword)
    counters = Counters()
    counters.cb = ctypes.sizeof(counters)
    if not inspect(current(), ctypes.byref(counters), counters.cb):
        raise ctypes.WinError()
    return counters.peak


answer = dict(
    utc=datetime.now(timezone.utc).isoformat(), runtime=platform.python_version(), host=platform.platform(),
    replay_sha256=digest(replay), pairs_sha256=digest(pairs), benchmark_sha256=digest(Path(__file__)),
    compare_scope="offline K5 conditional workload; each method selects its own frozen five references",
    load_and_integrity_ms=load_ms, combined_process_peak_rss_bytes=windows_peak_rss(),
    checked_case_method_results=len(records), methods={}, batches=batches)
for method in methods:
    matched = [r for r in records if r["method"] == method]
    answer["methods"][method] = {
        **{k:stats([r[k] for r in matched]) for k in matched[0] if k.endswith("_ms")},
        "batch_ms":stats([r["ms"] for r in batches if r["method"] == method])}
(output / "pipeline.json").write_text(json.dumps(answer, indent=2) + "\n", encoding="utf8")
with (output / "pipeline-cases.csv").open("w", encoding="utf8", newline="") as fp:
    writer = csv.DictWriter(fp, fieldnames=records[0])
    writer.writeheader()
    writer.writerows(records)
print(json.dumps(answer, indent=2))
