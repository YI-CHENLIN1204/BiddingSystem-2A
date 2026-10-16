# Project #8 only. Destructive to this explicitly marked DEMO project's history status.
# Never use against production projects or Project #11.
param([string]$BaseUrl = 'http://127.0.0.1:8081', [int]$ProjectId = 8)
$ErrorActionPreference = 'Stop'
if ($ProjectId -ne 8) { throw 'Safety stop: only DEMO Project #8 is permitted.' }
$base = $BaseUrl.TrimEnd('/')
$projectUri = "$base/api/projects/$ProjectId"
function Read-Project {
    Invoke-RestMethod -Uri $projectUri -Method Get -TimeoutSec 30
}
function Read-History {
    Invoke-RestMethod -Uri "$base/api/projects/$ProjectId/historical-data" -Method Get -TimeoutSec 30
}
function Get-Status($p) {
    if ($p.PSObject.Properties.Name -contains 'historyStatus') { return [string]$p.historyStatus }
    throw 'API response does not contain historyStatus; do not change data.'
}
function Confirm-State([string]$expected, [int]$expectedHistory) {
    $p = Read-Project
    $actual = Get-Status $p
    $data = Read-History
    # Count must be checked at the data level; do not treat grouped material summaries as row counts.
    Write-Host "Status: $actual; history API returned; expected: $expected"
    if ($actual -ne $expected) { throw "Unexpected status: $actual (expected $expected)" }
    return $data
}
function Request-Transition([string]$action, [string]$expectedStatus) {
    try {
        Invoke-RestMethod -Uri "$base/api/projects/$ProjectId/$action" -Method Post -TimeoutSec 90 | Out-Null
    } catch {
        Write-Warning "$action returned an error: $($_.Exception.Message)"
        $status = 'UNKNOWN'
        try { $status = Get-Status (Read-Project) } catch {}
        Write-Warning "Current authoritative status: $status. No automatic retry."
        throw 'Stop: inspect database and logs before any further write.'
    }
    $status = Get-Status (Read-Project)
    if ($status -ne $expectedStatus) { throw "Transition returned but status is $status; expected $expectedStatus" }
    Write-Host "$action => $status" -ForegroundColor Green
}
$p = Read-Project
if ($p.projectId -ne $ProjectId) { throw 'Unexpected project ID.' }
if (($p.projectName -notlike '*DEMO_2A_20261015*') -or (Get-Status $p) -ne 'Draft') {
    throw 'Safety stop: project is not the expected DEMO in Draft.'
}
Write-Host "DEMO project #8 verified; starting publish/reopen test."
try {
    Request-Transition 'publish-history' 'Approved'
    Request-Transition 'reopen-history-review' 'Draft'
    Request-Transition 'publish-history' 'Approved'
    Request-Transition 'reopen-history-review' 'Draft'
    $end = Read-Project
    if ((Get-Status $end) -ne 'Draft') { throw 'Final status mismatch' }
    Write-Host 'Completed two cycles; DEMO #8 returned to Draft. Validate transaction counts and audit logs in Supabase.' -ForegroundColor Green
} catch {
    Write-Error $_
    exit 1
}
