# Safety-only preflight. Does not start services or connect to Supabase.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$composeFile = Join-Path $root 'compose.fault-test.yaml'
$yaml = Get-Content -Raw -LiteralPath $composeFile
if ($yaml -notmatch 'name:\s*bidding-2a-fault' -or
    $yaml -notmatch '127\.0\.0\.1:18080:8080' -or
    $yaml -notmatch 'internal:\s*true' -or
    $yaml -notmatch 'Host=db;Port=5432;Database=bidding_fault' -or
    $yaml -match 'supabase\.com') {
  throw 'Unsafe fault stack configuration. Refusing to proceed.'
}
$had = Test-Path Env:FAULT_DB_PASSWORD
$old = if ($had) { $env:FAULT_DB_PASSWORD } else { $null }
try {
  $env:FAULT_DB_PASSWORD = [guid]::NewGuid().ToString('N')
  docker compose -f $composeFile config --quiet
  if ($LASTEXITCODE -ne 0) { throw 'Docker Compose configuration invalid.' }
  Write-Host 'PASS: isolated stack config and boundaries checked. No services started.' -ForegroundColor Green
  Write-Host 'NEXT: schema-only local fixture is required before invoking real API. This preflight does not certify application integration.'
} finally {
  if ($had) { $env:FAULT_DB_PASSWORD = $old }
  else { Remove-Item Env:FAULT_DB_PASSWORD -ErrorAction SilentlyContinue }
}
