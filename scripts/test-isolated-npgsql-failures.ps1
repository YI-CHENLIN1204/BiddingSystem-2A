# Run only in isolated, disposable Docker network and PostgreSQL. Never use Supabase.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$id = [guid]::NewGuid().ToString('N').Substring(0,12)
$network = "bid2a-fault-net-$id"
$dbName = "bid2a-fault-db-$id"
$password = [guid]::NewGuid().ToString('N')
try {
    docker network create $network | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create isolated network' }
    docker run -d --name $dbName --network $network -e "POSTGRES_PASSWORD=$password" postgres:17 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start disposable database' }
    $ready = $false
    for ($i=0; $i -lt 40; $i++) {
        docker exec $dbName pg_isready -U postgres | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Disposable PostgreSQL not ready' }
    $cs = "Host=$dbName;Port=5432;Database=postgres;Username=postgres;Password=$password;Pooling=false;SSL Mode=Disable;Timeout=10;Command Timeout=10"
    docker run --rm --network $network -e "TEST_POSTGRES_CONNECTION=$cs" -v "${root}:/src" -w /src/tests/ConnectionFaultHarness mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Npgsql isolated integration test failed' }
    Write-Host 'PASS: isolated Npgsql integration harness completed.' -ForegroundColor Green
} finally {
    docker rm -f $dbName 2>$null | Out-Null
    docker network rm $network 2>$null | Out-Null
}
