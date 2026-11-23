"""Benchmark real estimation POSTs on disposable synthetic data."""
import json, math, os, statistics, time, urllib.request, urllib.error
from pathlib import Path

assert os.environ.get("GITHUB_ACTIONS") == "true"
root = Path("evidence/api")
pid = int(os.environ["RESEARCH_SERVER_PID"])
def rss(field):
    line = next(line for line in Path(f"/proc/{pid}/status").read_text().splitlines()
                if line.startswith(field + ":"))
    return int(line.split()[1])

def request(path, data=None, content_type="application/json"):
    call = urllib.request.Request("http://127.0.0.1:18081" + path, data,
                                  headers={"Content-Type": content_type})
    started = time.perf_counter()
    try:
        with urllib.request.urlopen(call, timeout=90) as response:
            return response.status, json.load(response), (time.perf_counter()-started)*1000
    except urllib.error.HTTPError as exc:
        return exc.code, {"error": exc.read().decode(errors="replace")}, (time.perf_counter()-started)*1000
    except (TimeoutError, urllib.error.URLError) as exc:
        return 0, {"error": str(exc)}, (time.perf_counter()-started)*1000

def upload(n, iteration):
    boundary = "research2at08xlsx"
    fields = dict(caseName=f"T08 synthetic scale {n} trial {iteration}", projectType="住宅", structureType="RC",
                  city="測試縣市", totalFloorArea=1000, siteArea=250, floorCount=5,
                  basementFloorCount=1, projectDate="2026-01-01")
    data = b"".join(f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode()
                    for k, v in fields.items())
    data += (f'--{boundary}\r\nContent-Disposition: form-data; name="excelFile"; filename="t08-{n}.xlsx"\r\n'
             'Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\r\n\r\n').encode()
    data += (root / f"t08-{n}.xlsx").read_bytes() + f'\r\n--{boundary}--\r\n'.encode()
    return request("/api/analysis-cases/from-upload", data, "multipart/form-data; boundary="+boundary)

def write_jsonl(name, data):
    (root/name).write_text("".join(json.dumps(row,ensure_ascii=False)+"\n" for row in data),encoding="utf-8")

import csv, platform, re
from decimal import Decimal
from collections import Counter

n = int(os.environ["RESEARCH_SCALE_ROWS"])
warmups = 2
trials = 12
assert n in (500, 1000)
initial = rss("VmRSS")
log = root / "server.log"
cpu = next((x.split(":", 1)[1].strip() for x in Path("/proc/cpuinfo").read_text().splitlines()
            if x.startswith("model name")), platform.processor())
meta = dict(rows=n, pid=pid, cpu=cpu, logical_cpus=os.cpu_count(), os=platform.platform(),
            initial_ready_rss_kib=initial, history_projects=32, historical_transactions=64,
            warmups=warmups, measured_trials=trials, order=["excel-import", "quantity-post", "price-post"],
            db_metric="Sum of three EF SaveChangesAsync calls plus CommitAsync; includes EF change tracking and network",
            database=os.environ.get("RESEARCH_PG_VERSION", "PostgreSQL 17 service"))
(root/"scale-environment.json").write_text(json.dumps(meta,indent=2),encoding="utf-8")

records, seen_cases, seen_versions, seen_runs = [], set(), set(), set()
def record(trial, route, status, body, ms, state, reason, phase_ms=None):
    row = dict(rows=n, trial=trial, warmup=trial<warmups, route=route, status=status, outcome=state,
               reason=reason, elapsed_ms=ms, db_write_commit_ms=phase_ms, rss_kib=rss("VmRSS"),
               peak_so_far_kib=rss("VmHWM"), response=body)
    records.append(row)
    with (root/"scale-samples.jsonl").open("a",encoding="utf-8") as f:
        f.write(json.dumps(row,ensure_ascii=False)+"\n")

def interval_valid(values):
    try:
        a,m,b = (Decimal(str(x)) for x in values)
        return all(x.is_finite() for x in (a,m,b)) and 0 < a <= m <= b
    except (ValueError, ArithmeticError, TypeError):
        return False

for trial in range(warmups+trials):
    status, obj, ms = upload(n,trial)
    cid, vid = obj.get("analysisCaseId"), obj.get("analysisBidVersionId")
    timing = re.findall(rf"T08_DB {cid} ([\d.]+)", log.read_text(encoding="utf-8",errors="replace"))
    good = (status==200 and obj.get("itemCount")==n and isinstance(cid,int) and isinstance(vid,int)
            and cid not in seen_cases and vid not in seen_versions and len(timing)==1)
    # GET the persisted version before estimation to verify count and line uniqueness.
    base = f"/api/analysis-cases/{cid}/versions/{vid}"
    failure_reason = "Import status, count, ID uniqueness or DB timing mismatch"
    if good:
        read_status, version, _ = request(base)
        detail = version.get("items") or []
        if not detail:
            detail = next((v for v in version.values() if isinstance(v,list) and len(v)==n), [])
        names = [x.get("itemName") or x.get("name") for x in detail]
        good = read_status==200 and len(names)==n and None not in names and len(set(names))==n
        obj["verifiedPersistedRows"] = len(detail)
        obj["verifiedUniqueNames"] = len(set(names))
        failure_reason = "Persisted GetVersion line count or uniqueness mismatch"
    record(trial,"excel-import",status,obj,ms,"pass" if good else "error",
           "" if good else failure_reason,float(timing[0]) if len(timing)==1 else None)
    if not good:
        for route in ("quantity-post","price-post"):
            record(trial,route,0,{},None,"blocked","Import did not pass")
        continue
    seen_cases.add(cid)
    seen_versions.add(vid)
    for name, endpoint, payload in (
            ("quantity-post","quantity-analysis",{"topCount":8}),
            ("price-post","major-price-analysis",{"topCount":8,"majorCategory":"結構體工程"})):
        status,obj,ms=request(base+"/"+endpoint,json.dumps(payload,ensure_ascii=False).encode())
        result=obj.get("result") or {}
        run=obj.get("analysisRunId")
        valid_id=isinstance(run,int) and run not in seen_runs
        if isinstance(run,int): seen_runs.add(run)
        if status != 200 or not valid_id:
            state, reason = "error", "HTTP failure or missing/duplicate run ID"
        elif name=="quantity-post":
            items = result.get("items") or []
            calculated = [x for x in items if x.get("range")]
            usable = result.get("analyzedGroupCount")==2
            valid = usable and len(calculated)==2 and sorted(x.get("observedQuantity") for x in calculated)==[5*n,50*n] and all(
                interval_valid([x["range"].get(k) for k in ("lowerQuantity","medianQuantity","upperQuantity")])
                for x in calculated)
            state, reason = ("pass","") if valid else (
                ("error","Invalid quantity interval") if usable else ("abstain","Two material groups were not analyzed"))
        else:
            usable=result.get("hasEnoughSamples") is True
            valid=usable and result.get("observedAmount")==15000*n and interval_valid([result.get(k) for k in ("lowerAmount","medianAmount","upperAmount")])
            state, reason = ("pass","") if valid else (
                ("error","Invalid price interval") if usable else ("abstain","Insufficient valid price references"))
        record(trial,name,status,obj,ms,state,reason)

def stats(values):
    values=sorted(values)
    return dict(p50=statistics.median(values),p95=values[math.ceil(.95*len(values))-1]) if values else dict(p50=None,p95=None)
summary={}
for name in meta["order"]:
    subset=[x for x in records if x["route"]==name and not x["warmup"]]
    approved=[x for x in subset if x["outcome"]=="pass"]
    counts=Counter(x["outcome"] for x in subset)
    summary[name]=dict(latency_ms=stats(x["elapsed_ms"] for x in approved),
        db_write_commit_ms=stats(x["db_write_commit_ms"] for x in approved if x["db_write_commit_ms"] is not None),
        n=len(subset), outcomes=dict(counts), success_rate=counts["pass"]/len(subset),
        abstention_rate=counts["abstain"]/len(subset),
        reasons=dict(Counter(x["reason"] for x in subset if x["reason"])))
steady=statistics.median(x["rss_kib"] for x in records if not x["warmup"])
time.sleep(2)
after=rss("VmRSS")
summary["memory"]=dict(initial_ready_kib=initial, steady_sample_median_kib=steady,
    peak_kib=rss("VmHWM"), after_two_second_idle_kib=after, delta_after_minus_initial_kib=after-initial,
    latter_half_minus_first_half_kib=statistics.median(x["rss_kib"] for x in records if x["trial"]>=warmups+trials//2)
    -statistics.median(x["rss_kib"] for x in records if warmups<=x["trial"]<warmups+trials//2))
(root/"scale-summary.json").write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding="utf-8")
with (root/"scale-samples.csv").open("w",encoding="utf-8",newline="") as stream:
    keys=[k for k in records[0] if k!="response"]
    writer=csv.DictWriter(stream,fieldnames=keys,extrasaction="ignore")
    writer.writeheader()
    writer.writerows(records)
print(json.dumps(dict(environment=meta,summary=summary),ensure_ascii=False),flush=True)
assert all(x["outcome"]=="pass" for x in records)
