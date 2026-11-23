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

def request(label, path, *, post=False, key=None, form=None, expected=200, required=True, token=None, multipart_file=None, multipart_fields=None):
    headers = {"Idempotency-Key": key} if key else {}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    data = urllib.parse.urlencode(form).encode() if form is not None else None
    if multipart_file is not None:
        assert form is None and multipart_fields is not None
        boundary = "research2a" + uuid4().hex
        chunks = []
        for field, value in multipart_fields.items():
            chunks.extend([
                f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"\r\n\r\n'.encode(),
                str(value).encode("utf-8"), b"\r\n"
            ])
        chunks.extend([
            f'--{boundary}\r\nContent-Disposition: form-data; name="excelFile"; filename="synthetic-final-bid.xlsx"\r\nContent-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\r\n\r\n'.encode(),
            Path(multipart_file).read_bytes(), b"\r\n", f'--{boundary}--\r\n'.encode()
        ])
        data = b"".join(chunks)
        headers["Content-Type"] = f"multipart/form-data; boundary={boundary}"
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

# T07: actual HTTP multipart -> parser -> isolated PostgreSQL -> Draft, with explicit sheet selection.
upload_form = {
    "projectName": "CI imported synthetic final bid", "projectType": "住宅",
    "structureType": "RC", "city": "測試縣市", "totalFloorArea": "1000",
    "siteArea": "250", "floorCount": "5", "basementFloorCount": "1",
    "projectMonth": "2026-01", "includedSheetNames": json.dumps(["結構體明細"], ensure_ascii=False)
}
file_path = "evidence/api/synthetic-final-bid.xlsx"
bad_sheets = dict(upload_form, includedSheetNames=json.dumps(["不存在的工作表"], ensure_ascii=False))
request("upload-invalid-sheet-rejected", "/api/Estimation/upload-final-bid",
        post=True, multipart_file=file_path, multipart_fields=bad_sheets, expected=400)
uploaded = request("upload-final-bid-multipart", "/api/Estimation/upload-final-bid",
                   post=True, multipart_file=file_path, multipart_fields=upload_form)[1]
assert uploaded["historyStatus"] == "Draft" and uploaded["totalCount"] == 2, uploaded
assert uploaded["historyInsertCount"] == 0 and uploaded["structuralPricedItemCount"] == 2, uploaded
imported_id = uploaded["projectId"]
assert request("imported-project-draft", f"/api/projects/{imported_id}")[1]["historyStatus"] == "Draft"
assert request("imported-project-draft-no-history", f"/api/projects/{imported_id}")[1]["historicalTransactionCount"] == 0
imported_rows = request("imported-items-selected-sheet-only", f"/api/projects/{imported_id}/final-bid-items")[1]["items"]
assert len(imported_rows) == 2, imported_rows
assert all(x["itemName"] != "不得匯入比價表" for x in imported_rows), imported_rows
request("duplicate-upload-hash-rejected", "/api/Estimation/upload-final-bid",
        post=True, multipart_file=file_path,
        multipart_fields=dict(upload_form, projectName="CI different name same file"), expected=409)
summary = request("imported-2a-summary", f"/api/projects/{imported_id}/2a-summary")[1]
print(json.dumps({"test": "import-upload-draft-safety", "passed": True,
                  "project_id": imported_id, "rows": len(imported_rows),
                  "history_insert_count": uploaded["historyInsertCount"]}), flush=True)

imported_project = f"/api/projects/{imported_id}"
material_ids = {"SD420W 鋼筋材料": fixture["steel"], "預拌混凝土": fixture["concrete"]}
for row in imported_rows:
    request("map-imported-item", "/api/Estimation/update-material-mapping", post=True,
            form=dict(finalBidItemId=row["finalBidItemId"], materialId=material_ids[row["itemName"]]))
mapped = request("imported-mapped-roundtrip", imported_project + "/final-bid-items")[1]["items"]
assert len(mapped) == 2 and all(row["mappingStatus"] == "Manual" for row in mapped), mapped
published_import = request("publish-imported-project", imported_project + "/publish-history",
                           post=True, key=str(uuid4()))[1]
assert published_import["historyStatus"] == "Approved"
approved_import = request("read-imported-approved", imported_project)[1]
assert approved_import["historyStatus"] == "Approved" and approved_import["historicalTransactionCount"] == 2, approved_import
reopened_import = request("reopen-imported-project", imported_project + "/reopen-history-review",
                          post=True, key=str(uuid4()))[1]
assert reopened_import["historyStatus"] == "Draft" and reopened_import["disabledHistoricalTransactionCount"] == 2
draft_import = request("read-imported-reopened-draft", imported_project)[1]
assert draft_import["historyStatus"] == "Draft" and draft_import["historicalTransactionCount"] == 0, draft_import

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
print(json.dumps(dict(test="supervisor-audit-events", passed=True,
                      project_id=fixture["project"], actions=actions)), flush=True)
import_actions = [row["actionType"] for row in audit["items"] if row.get("projectId") == imported_id]
assert import_actions.count("PublishHistoryBaseline") == import_actions.count("ReopenHistoryReview") == 1, import_actions
print(json.dumps(dict(test="import-to-reopen-full-chain", passed=True, project_id=imported_id,
                      approved_history=2, reopened_active_history=0, actions=import_actions)), flush=True)
elapsed = [request("benchmark-read", project)[2] for _ in range(30)]
print(json.dumps(dict(test="read-http-summary", n=len(elapsed), p50_ms=statistics.median(elapsed),
                      p95_ms=sorted(elapsed)[math.ceil(.95*len(elapsed))-1],
                      anonymous_audit_status=anonymous)), flush=True)
# Supplemental API-only benchmark for real imported project 2A summary.
summary_elapsed = [request("benchmark-2a-summary", f"/api/projects/{imported_id}/2a-summary")[2]
                   for _ in range(30)]
print(json.dumps(dict(test="summary-http-summary", n=len(summary_elapsed),
                      p50_ms=statistics.median(summary_elapsed),
                      p95_ms=sorted(summary_elapsed)[math.ceil(.95*len(summary_elapsed))-1])), flush=True)
if anonymous not in (401, 403) or reader != 403 or invalid != 401:
    raise SystemExit("Audit access did not meet the role policy.")
