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
        with urllib.request.urlopen(call, timeout=30) as response:
            return response.status, json.load(response), (time.perf_counter()-started)*1000
    except urllib.error.HTTPError as exc:
        return exc.code, {"error": exc.read().decode(errors="replace")}, (time.perf_counter()-started)*1000
    except (TimeoutError, urllib.error.URLError) as exc:
        return 0, {"error": str(exc)}, (time.perf_counter()-started)*1000

def upload(n):
    boundary = "research2at08xlsx"
    fields = dict(caseName=f"T08 synthetic scale {n}", projectType="住宅", structureType="RC",
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

samples, setups = [], []
for n in (2,20,100):
    status, obj, ms = upload(n)
    setups.append(dict(rows=n,status=status,elapsed_ms=ms,response=obj))
    write_jsonl("estimate-setup.jsonl",setups)
    assert status==200 and obj["itemCount"]==n, obj
    base = f'/api/analysis-cases/{obj["analysisCaseId"]}/versions/{obj["analysisBidVersionId"]}'
    routes = [("read-version", "", None),
              ("estimate-quantity", "/quantity-analysis", {"topCount":8}),
              ("estimate-major-price", "/major-price-analysis", {"topCount":8,"majorCategory":"結構體工程"})]
    for run in range(22):
        for name, endpoint, payload in routes:
            data = json.dumps(payload,ensure_ascii=False).encode() if payload is not None else None
            status, obj, ms = request(base+endpoint,data)
            ok = status==200 and (payload is None or (isinstance(obj.get("analysisRunId"),int) and "result" in obj))
            result = obj.get("result") or {}
            if name == "estimate-quantity":
                ok = ok and result.get("analyzedGroupCount", 0) == 2
            elif name == "estimate-major-price":
                ok = ok and result.get("hasEnoughSamples") is True and result.get("lowerAmount") is not None
            samples.append(dict(rows=n,route=name,iteration=run,warmup=run<2,status=status,passed=ok,
                                elapsed_ms=ms,rss_kib=rss("VmRSS"),process_peak_so_far_kib=rss("VmHWM"),
                                response=obj))
            write_jsonl("estimate-samples.jsonl",samples)
summary=[]
for n in (2,20,100):
    for route in ("read-version","estimate-quantity","estimate-major-price"):
        cohort=[x for x in samples if x["rows"]==n and x["route"]==route and not x["warmup"]]
        latency=sorted(x["elapsed_ms"] for x in cohort if x["passed"])
        summary.append(dict(rows=n,route=route,n=len(cohort),failed=len(cohort)-len(latency),
            failure_rate=(len(cohort)-len(latency))/len(cohort),scope="isolated synthetic A0 HTTP",
            p50_ms=statistics.median(latency) if latency else None,
            p95_ms=latency[math.ceil(.95*len(latency))-1] if latency else None,
            max_sampled_rss_kib=max(x["rss_kib"] for x in cohort),
            process_peak_so_far_kib=max(x["process_peak_so_far_kib"] for x in cohort)))
(root/"estimate-summary.json").write_text(json.dumps(summary,indent=2)+"\n",encoding="utf-8")
print(json.dumps(summary),flush=True)
assert all(x["passed"] for x in samples)
