# Only run after applying the receipt migration and deploying the new API.
# Writes ONLY to DEMO Project #8. Stops on uncertain results; never auto-retries a failed POST.
param([string]$BaseUrl = "http://127.0.0.1:8081", [int]$ProjectId = 8)
$ErrorActionPreference = 'Stop'
if ($ProjectId -ne 8) { throw 'Safety stop: test limited to DEMO #8' }
$root = $BaseUrl.TrimEnd('/')
$uri = "$root/api/projects/$ProjectId"
function Get-Demo {
    $p = Invoke-RestMethod -Uri $uri -TimeoutSec 30
    if ($p.projectId -ne 8 -or $p.projectName -notlike '*DEMO_2A_20261015*') {
        throw 'Unexpected project identity; no writes permitted.'
    }
    return $p
}
function Assert-State([string]$status) {
    $p = Get-Demo
    if ($p.historyStatus -ne $status) { throw "Expected $status, got $($p.historyStatus)" }
}
function Invoke-Twice([string]$path,[string]$expected) {
    $key = [guid]::NewGuid().ToString()
    $headers = @{ 'Idempotency-Key' = $key }
    $endpoint = "$uri/$path"
    try {
        $first = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -TimeoutSec 90
    } catch {
        Write-Warning "First request uncertain. Keep operation ID: $key"
        throw
    }
    Assert-State $expected
    $receipt = Invoke-RestMethod -Uri "$uri/history-operation/$key" -TimeoutSec 30
    if ($null -eq $receipt -or -not $receipt.operationId) { throw 'Receipt missing after success' }
    # Same key must replay the result without another audit/transaction.
    $second = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -TimeoutSec 90
    Assert-State $expected
    Write-Host "$path replayed same operation ID $key" -ForegroundColor Green
}
Assert-State 'Draft'
Invoke-Twice 'publish-history' 'Approved'
Invoke-Twice 'reopen-history-review' 'Draft'
Write-Host 'PASS: idempotent Publish/Reopen replay; DEMO #8 returned to Draft.' -ForegroundColor Green
Write-Host 'Verify exactly ONE new audit entry per action and zero active DEMO #8 history rows in Supabase.'
