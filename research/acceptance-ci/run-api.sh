#!/usr/bin/env bash
set -euo pipefail
mkdir -p evidence/api
trap 'find evidence/api -type f ! -name SHA256SUMS -print0 | sort -z | xargs -0 -r sha256sum > evidence/api/SHA256SUMS' EXIT
[[ "$GITHUB_ACTIONS" == true && "$RESEARCH_PORT" =~ ^[0-9]+$ ]]
export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=$RESEARCH_PORT;Database=research_acceptance;Username=research_fixture;Password=disposable-ci-only;SSL Mode=Disable;Pooling=false"
export ASPNETCORE_ENVIRONMENT=Development
cp research/2a_synthetic/kde_comparison/t00t11-followup-20261123/UnitConversionService.cs 崴正/Services/UnitConversionService.cs
dotnet run --project research/acceptance-ci/Fixture.csproj -c Release 2>&1 | tee evidence/api/fixture.log
dotnet run --project 崴正/崴正.csproj -c Release --no-build --no-launch-profile --urls http://127.0.0.1:18081 > evidence/api/server.log 2>&1 &
server=$!
trap 'kill "$server" 2>/dev/null || true; find evidence/api -type f ! -name SHA256SUMS -print0 | sort -z | xargs -0 -r sha256sum > evidence/api/SHA256SUMS' EXIT
for attempt in {1..40}; do
  if curl -fsS http://127.0.0.1:18081/swagger/v1/swagger.json -o /dev/null; then break; fi
  kill -0 "$server"
  sleep 1
done
python research/acceptance-ci/probe-api.py | tee evidence/api/http.jsonl
