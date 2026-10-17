# Read-only idempotency receipt API regression.
# Does not Publish/Reopen or modify any database data.
param([string]$BaseUrl = "http://127.0.0.1:8081")
$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')
$receipts = @(
  @{ Id = '5864ea2c-f09f-48d5-ad43-4be0d84aa811'; Action = 'PublishHistory' },
  @{ Id = '275415d2-5e6f-4f33-8f7e-22633e7ef1a1'; Action = 'ReopenHistoryReview' }
)
foreach ($entry in $receipts) {
  $uri = "$base/api/projects/8/history-operation/$($entry.Id)"
  $result = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 30
  if ($result.operationId -ne $entry.Id -or $result.projectId -ne 8 -or $result.actionType -ne $entry.Action) {
    throw "FAIL: receipt mismatch for $($entry.Id)"
  }
  Write-Host "PASS: receipt $($entry.Action)" -ForegroundColor Green
}
$project = Invoke-RestMethod -Uri "$base/api/projects/11" -TimeoutSec 30
if ($project.historyStatus -ne 'Approved') { throw 'FAIL: Project #11 unexpectedly changed' }
$demo = Invoke-RestMethod -Uri "$base/api/projects/8" -TimeoutSec 30
if ($demo.historyStatus -ne 'Draft') { throw 'FAIL: DEMO #8 unexpectedly changed' }
Write-Host 'PASS: DEMO #8 Draft; Project #11 Approved. No writes issued.' -ForegroundColor Green
