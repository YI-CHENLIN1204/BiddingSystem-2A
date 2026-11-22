"""Only call the ephemeral runner API using synthetic identifiers."""
import json
import math
import os
import statistics
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from time import perf_counter
from uuid import uuid4

fixture = json.loads(Path("evidence/api/fixture.json").read_text())
project = f"/api/projects/{fixture['project']}"
other = f"/api/projects/{fixture['other']}"

def request(label, path, *, post=False, key=None, form=None, expected=200, required=True, token=None):
    headers = {"Idempotency-Key": key} if key else {}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    data = urllib.parse.urlencode(form).encode() if form is not None else None
    call = urllib.request.Request("http://127.0.0.1:18081" + path, data,
                                  headers, method="POST" if post else "GET")
    start = perf_counter()
    try:
        response = urllib.request.urlopen(call, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        code, raw = response.status, response.read().decode()
    elapsed = (perf_counter() - start) * 1000
    allowed = (expected,) if isinstance(expected, int) else expected
    passed = code in allowed
    print(json.dumps(dict(test=label, status=code, passed=passed, elapsed_ms=elapsed,
                          body=raw if label != "benchmark-read" else None)), flush=True)
    if required:
        assert passed, (label, code, raw)
    return code, json.loads(raw) if raw else None, elapsed

anonymous = request("anonymous-audit-policy", "/api/audit-logs",
                    expected=(401, 403), required=False)[0]
assert request("draft-read", project)[1]["historyStatus"] == "Draft"
mapping = dict(finalBidItemId=fixture["item"], materialId=fixture["steel"])
request("manual-mapping", "/api/Estimation/update-material-mapping", post=True, form=mapping)
items = request("read-manual-mapping", project + "/final-bid-items")[1]["items"]
assert len(items) == 1 and items[0]["mappingStatus"] == "Manual"
assert request("separate-project-items", other + "/final-bid-items")[1]["items"] == []
key = str(uuid4())
published = request("publish", project + "/publish-history", post=True, key=key)[1]
assert request("read-approved", project)[1]["historyStatus"] == "Approved"
assert request("repeat-publish", project + "/publish-history", post=True, key=key)[1] == published
request("cross-project-key-rejected", other + "/publish-history", post=True, key=key,
        expected=409)
key = str(uuid4())
reopened = request("reopen", project + "/reopen-history-review", post=True, key=key)[1]
assert request("read-reopened", project)[1]["historyStatus"] == "Draft"
assert request("repeat-reopen", project + "/reopen-history-review", post=True, key=key)[1] == reopened
assert request("other-project-still-draft", other)[1]["historyStatus"] == "Draft"
reader = request("reader-audit-policy", "/api/audit-logs", expected=403,
                 required=False, token=os.environ["RESEARCH_AUDIT_READER_TOKEN"])[0]
invalid = request("invalid-audit-credential", "/api/audit-logs", expected=401,
                  required=False, token="invalid")[0]
audit = request("audit-read", "/api/audit-logs",
                token=os.environ["RESEARCH_AUDIT_SUPERVISOR_TOKEN"])[1]
rows = [row for row in audit["items"] if row.get("projectId") == fixture["project"]]
actions = [row["actionType"] for row in rows]
assert sum("publish" in action.lower() for action in actions) == 1, actions
assert sum("reopen" in action.lower() for action in actions) == 1, actions
elapsed = [request("benchmark-read", project)[2] for _ in range(30)]
print(json.dumps(dict(test="read-http-summary", n=len(elapsed), p50_ms=statistics.median(elapsed),
                      p95_ms=sorted(elapsed)[math.ceil(.95*len(elapsed))-1],
                      anonymous_audit_status=anonymous)), flush=True)
if anonymous not in (401, 403) or reader != 403 or invalid != 401:
    raise SystemExit("Audit access did not meet the role policy.")
