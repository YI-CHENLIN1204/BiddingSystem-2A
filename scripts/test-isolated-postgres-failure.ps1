# Disposable PostgreSQL failure test. No Supabase, no application DB, no published port.
# Requires Docker and postgres:17. Uses a unique container name and always removes it.
param()
$ErrorActionPreference = 'Stop'
$name = 'bid2a-fault-' + [guid]::NewGuid().ToString('N').Substring(0,10)
try {
    $pw = [guid]::NewGuid().ToString('N')
    $id = docker run -d --name $name --network none -e "POSTGRES_PASSWORD=$pw" postgres:17
    if ($LASTEXITCODE -ne 0) { throw 'Unable to start disposable PostgreSQL container.' }
    $ready = $false
    foreach ($i in 1..30) {
        docker exec $name pg_isready -U postgres | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Disposable PostgreSQL did not become ready.' }
    $init = 'CREATE TABLE fault_test(id int PRIMARY KEY);'
    docker exec $name psql -U postgres -v ON_ERROR_STOP=1 -c $init
    if ($LASTEXITCODE -ne 0) { throw 'Failed to initialize disposable table.' }
    # Force a timeout INSIDE an explicit transaction after a successful insert.
    $sql = 'BEGIN; INSERT INTO fault_test VALUES (1); SET LOCAL statement_timeout = 100; SELECT pg_sleep(1); COMMIT;'
    # Expected PostgreSQL error goes to stderr; PowerShell 5.1 must not treat it as a script-stopping error.
    $ErrorActionPreference = 'Continue'
    $result = docker exec $name psql -U postgres -v ON_ERROR_STOP=0 -c $sql 2>&1
    $ErrorActionPreference = 'Stop'
    $result | ForEach-Object { Write-Host $_ }
    $text = ($result | Out-String)
    if ($text -notmatch 'statement timeout') { throw 'Expected statement timeout was not observed.' }
    $count = docker exec $name psql -U postgres -A -t -v ON_ERROR_STOP=1 -c 'SELECT count(*) FROM fault_test;'
    if ($LASTEXITCODE -ne 0) { throw 'Post-failure read failed.' }
    if (($count | Out-String).Trim() -ne '0') { throw 'FAIL: partial data was committed.' }
    Write-Host 'PASS: transaction was rolled back; no partial row exists.' -ForegroundColor Green
    Write-Host 'NOTE: This proves PostgreSQL rollback-on-error only. Application Npgsql disconnect/retry behavior still requires isolated integration testing.'
}
finally {
    docker rm -f $name 2>$null | Out-Null
}
