# Read-only verification following a pre-commit failure on isolated Project #1.
param([string]$BaseUrl = "http://127.0.0.1:18080")
$ErrorActionPreference = 'Stop'
$project = Invoke-RestMethod -Uri "$($BaseUrl.TrimEnd('/'))/api/projects/1" -TimeoutSec 20
if ($project.projectName -notlike '[[]FAULT_FIXTURE_ONLY[]]*') { throw 'Unsafe fixture identity' }
if ($project.historyStatus -ne 'Draft') { throw "Expected Draft, found $($project.historyStatus)" }
$query = @'
SELECT
 (SELECT count(*) FROM historical_transaction WHERE project_master_id=1 AND NOT is_deleted) AS active_history,
 (SELECT count(*) FROM history_operation_receipt WHERE project_id=1 AND action_type='PublishHistory') AS publish_receipts,
 (SELECT count(*) FROM audit_log WHERE project_id=1 AND action_type='PublishHistoryBaseline') AS publish_audits;
'@
docker compose -f compose.fault-test.yaml exec -T db psql -U fault_user -d bidding_fault -v ON_ERROR_STOP=1 -c $query
if($LASTEXITCODE -ne 0){throw 'SQL check failed'}
Write-Host 'Review totals against BEFORE-test baseline. No write was issued by this verifier.'
