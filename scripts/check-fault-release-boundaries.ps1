# Read-only, fail-closed check of repo production and isolated test configuration.
$ErrorActionPreference = 'Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$production=Get-Content -Raw -LiteralPath (Join-Path $root 'compose.yaml')
if($production -match 'FAULT_TEST_DROP_BEFORE_COMMIT|compose\.fault-drop|bidding_fault') {
  throw 'Production compose references fault test functionality'
}
$script=Get-Content -Raw -LiteralPath (Join-Path $root '崴正/Services/ProjectManagementService.cs')
if($script -notmatch 'FAULT_TEST_DROP_BEFORE_COMMIT' -or $script -notmatch 'FAULT_TEST_DROP_BEFORE_COMMIT refused: isolated fixture guard failed') {
  throw 'Failpoint guard missing or changed; manual review needed'
}
Write-Host 'PASS: static production compose check and presence of fail-closed guard'
Write-Host 'LIMIT: static checks DO NOT prove other deployment manifests or runtime env are safe.'
Write-Host 'RELEASE GATE: remove or compile-exclude fault-injection logic from production artifact before release.'
