# Concurrent state transition test: ONLY DEMO Project #8, not production data.
# Requires the API version with per-project advisory transaction locking.
param([string]$BaseUrl = "http://127.0.0.1:8081", [int]$ProjectId = 8)
$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1 does not load this assembly automatically.
Add-Type -AssemblyName System.Net.Http
if ($ProjectId -ne 8) { throw 'Safety guard: only DEMO project #8 is permitted.' }
$base = $BaseUrl.TrimEnd('/')
$uri = "$base/api/projects/$ProjectId"
function Get-Project {
    Invoke-RestMethod -Uri $uri -TimeoutSec 30
}
function Ensure-State([string]$expected) {
    $p = Get-Project
    if (($p.projectId -ne 8) -or ($p.projectName -notlike '*DEMO_2A_20261015*')) {
        throw 'Safety guard: project ID/name mismatch.'
    }
    if ($p.historyStatus -ne $expected) { throw "Expected $expected; got $($p.historyStatus). Do not retry writes." }
}
function Concurrent-Transition([string]$endpoint, [string]$before, [string]$after) {
    Ensure-State $before
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(90)
    try {
        $target = "$base/api/projects/$ProjectId/$endpoint"
        $empty1 = [System.Net.Http.ByteArrayContent]::new([byte[]]@())
        $empty2 = [System.Net.Http.ByteArrayContent]::new([byte[]]@())
        try {
            $a = $client.PostAsync($target, $empty1)
            $b = $client.PostAsync($target, $empty2)
            [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($a, $b))
            $codes = @([int]$a.Result.StatusCode, [int]$b.Result.StatusCode)
            Write-Host "$endpoint response codes: $($codes -join ', ')"
            if ((@($codes | Where-Object { $_ -ge 200 -and $_ -lt 300 }).Count -ne 1) -or
                (@($codes | Where-Object { $_ -eq 409 }).Count -ne 1)) {
                throw 'Unexpected concurrent responses; inspect Supabase state and logs. Do not retry.'
            }
            Ensure-State $after
        } finally { $empty1.Dispose(); $empty2.Dispose() }
    } finally { $client.Dispose() }
}
Ensure-State 'Draft'
Write-Host 'Starting two simultaneous publishes on DEMO #8.'
Concurrent-Transition 'publish-history' 'Draft' 'Approved'
Write-Host 'Starting two simultaneous reopens on DEMO #8.'
Concurrent-Transition 'reopen-history-review' 'Approved' 'Draft'
Write-Host 'Concurrent transition check passed. DEMO #8 returned to Draft.' -ForegroundColor Green
Write-Host 'Verify effective historical transactions = 0 and exactly one PublishHistoryBaseline and ReopenHistoryReview audit entry in Supabase.'
