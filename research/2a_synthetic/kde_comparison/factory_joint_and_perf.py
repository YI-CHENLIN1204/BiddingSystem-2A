import runpy, statistics, time, tracemalloc
from pathlib import Path

run = runpy.run_path(str(Path(__file__).with_name("replay_factory_a1.py")))
rows = run["test"]
interval, factor = run["interval"], run["factor"]
def workload():
    return [interval(row, factor, .3) for row in rows]
expected = workload()
times = []
for _ in range(20):
    start = time.perf_counter()
    actual = workload()
    times.append((time.perf_counter() - start) * 1000)
    assert actual == expected
tracemalloc.start()
workload()
_, peak = tracemalloc.get_traced_memory()
tracemalloc.stop()
print("KDE", len(rows), statistics.median(times), sorted(times)[18], peak)
cases = {}
for row in run["results"]:
    cases.setdefault(row["project"], {})[row["scope"]] = row
for quantity, price in ((.75,1.25),(1.25,.75),(.5,1.5),(1.5,.5)):
    print("Joint", quantity, price, [
        sum(all((r["truth"] * multiplier < r[method+"_low"] if multiplier < 1
                 else r["truth"] * multiplier > r[method+"_high"])
                for scope,r in case.items()
                for multiplier in [price if scope == "structure_price" else quantity])
            for case in cases.values()) for method in ("a0","a1")])
