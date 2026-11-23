"""Sequential, fresh-process benchmark of the frozen offline replay pipelines."""
import argparse
import csv
import ctypes
import hashlib
import json
import math
import os
import platform
import statistics
import subprocess
import sys
from pathlib import Path
from time import perf_counter

ROOT = Path(__file__).resolve().parent

def peak_rss_kib():
    if os.name != "nt":
        import resource
        number = resource.getrusage(resource.RUSAGE_SELF).ru_maxrss
        return int(number / 1024 if sys.platform == "darwin" else number)
    class Counters(ctypes.Structure):
        _fields_ = [("size", ctypes.c_uint32), ("pagefaults", ctypes.c_uint32)] + [
            (name, ctypes.c_size_t) for name in ("peak", "working", "quota_peak_paged", "quota_paged",
               "quota_peak_nonpaged", "quota_nonpaged", "pagefile", "peak_pagefile")]
    result = Counters()
    result.size = ctypes.sizeof(result)
    current = ctypes.windll.kernel32.GetCurrentProcess
    current.restype = ctypes.c_void_p
    get_memory = ctypes.windll.psapi.GetProcessMemoryInfo
    get_memory.argtypes = [ctypes.c_void_p, ctypes.POINTER(Counters), ctypes.c_uint32]
    if not get_memory(current(), ctypes.byref(result), result.size):
        raise ctypes.WinError()
    return result.peak // 1024

def child(method, source, destination):
    import runpy
    script = {"a0": "replay_factory_a0_b1.py", "a1": "replay_factory_a1.py",
              "b1_b2": "replay_factory_b1.py"}[method]
    sys.argv = [str(ROOT / script), str(source), str(destination)]
    start = perf_counter()
    runpy.run_path(str(ROOT / script), run_name="__main__")
    print(json.dumps(dict(seconds=perf_counter()-start, peak_process_rss_kib=peak_rss_kib())))

def main(source, b1_pairs, output):
    output.mkdir(parents=True, exist_ok=False)
    samples = []
    for round_number, order in enumerate((
            ("a0","a1","b1_b2"), ("b1_b2","a0","a1"), ("a1","b1_b2","a0"),
            ("a0","b1_b2","a1"))):
        for method in order:
            target = output / f"{round_number}-{method}"
            target.mkdir()
            if method != "b1_b2":
                (target / "factory_b1_pairs.csv").write_bytes(b1_pairs.read_bytes())
            proc = subprocess.run([sys.executable, str(Path(__file__).resolve()), "--child", method,
                                   str(source), str(target)], capture_output=True,
                                   encoding="utf-8", env=dict(os.environ,PYTHONIOENCODING="utf-8"))
            (target / "stdout.txt").write_text(proc.stdout, encoding="utf-8")
            (target / "stderr.txt").write_text(proc.stderr, encoding="utf-8")
            proc.check_returncode()
            sample = json.loads(proc.stdout.splitlines()[-1])
            samples.append(dict(round=round_number, warmup=round_number==0, method=method, **sample))
            print(samples[-1], flush=True)
    with (output / "samples.csv").open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, samples[0].keys())
        writer.writeheader()
        writer.writerows(samples)
    groups = {}
    for method in ("a0","a1","b1_b2"):
        rows = [r for r in samples if r["method"] == method and not r["warmup"]]
        times = sorted(r["seconds"] for r in rows)
        groups[method] = dict(n=len(times), p50_seconds=statistics.median(times),
             p95_seconds_nearest_rank=times[math.ceil(.95*len(times))-1],
             max_peak_process_rss_kib=max(r["peak_process_rss_kib"] for r in rows))
    result = dict(platform=platform.platform(), python=sys.version, logical_cpus=os.cpu_count(),
        source_manifest_sha256=hashlib.sha256((source/"manifest.json").read_bytes()).hexdigest(),
        b1_pairs_sha256=hashlib.sha256(b1_pairs.read_bytes()).hexdigest(), methods=groups,
        workload="Same 180-project frozen source; A0 reads B1 rows as its case index; A1 includes A0 replay and validation; B1/B2 includes 2 levels and ablation. Durations are pipeline costs, not equal-work rankings.")
    (output / "summary.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")

if __name__ == "__main__":
    if sys.argv[1:2] == ["--child"]:
        child(sys.argv[2], Path(sys.argv[3]), Path(sys.argv[4]))
    else:
        cli = argparse.ArgumentParser(description=__doc__)
        for n in ("source","b1_pairs","new_output"):
            cli.add_argument(n,type=Path)
        args = cli.parse_args()
        main(args.source.resolve(),args.b1_pairs.resolve(),args.new_output.resolve())
