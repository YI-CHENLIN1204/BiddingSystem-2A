# Read-only static checks. These are NOT a substitute for inspecting built artifacts.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$production = Get-Content -Raw -LiteralPath (Join-Path $root 'compose.yaml')
if ($production -match 'FAULT_TEST_DROP_BEFORE_COMMIT|compose\.fault-drop|bidding_fault|FAULT_INJECTION_TEST') {
    throw 'Production compose references fault testing'
}
$candidates = @(Get-ChildItem -Path $root -Filter 'ProjectManagementService.cs' -Recurse -File |
    Where-Object { $_.FullName -match '[\\/]Services[\\/]' })
if ($candidates.Count -ne 1) { throw "Expected one ProjectManagementService.cs, found $($candidates.Count)" }
$source = Get-Content -Raw -LiteralPath $candidates[0].FullName
$guards = ([regex]::Matches($source, '(?m)^#if FAULT_INJECTION_TEST\s*$')).Count
$ends = ([regex]::Matches($source, '(?m)^#endif\s*$')).Count
$failpoints = ([regex]::Matches($source, 'Environment.GetEnvironmentVariable\("FAULT_TEST_DROP_BEFORE_COMMIT"\)')).Count
if ($guards -ne 2 -or $failpoints -ne 2 -or $ends -lt 2) {
    throw 'Compile guards or fault injection count changed. Review manually.'
}
$dockerFiles = @(Get-ChildItem -Path $root -Filter 'Dockerfile' -Recurse -File)
$docker = $dockerFiles | Where-Object { $_.FullName -match '[\\/]Dockerfile$' -and $_.FullName -notmatch 'node_modules' } |
    Where-Object { (Get-Content -Raw $_.FullName) -match 'ARG FAULT_INJECTION_TEST=false' } | Select-Object -First 1
if (-not $docker) { throw 'Expected fault-safe backend Dockerfile not found' }
$override = Get-Content -Raw -LiteralPath (Join-Path $root 'compose.fault-build.yaml')
if ($override -notmatch 'FAULT_INJECTION_TEST') { throw 'Fault test build override not found' }
Write-Host 'PASS: default backend Docker build has opt-in fault compilation; static guard and production compose checks passed.'
Write-Host 'LIMIT: static checks do not prove compiled DLL contents, other manifests, or runtime configuration.'
