#!/usr/bin/env bash
set -euo pipefail
mkdir -p evidence/api
trap 'find evidence/api -type f ! -name SHA256SUMS -print0 | sort -z | xargs -0 -r sha256sum > evidence/api/SHA256SUMS' EXIT
[[ "$GITHUB_ACTIONS" == true && "$RESEARCH_PORT" =~ ^[0-9]+$ ]]
export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=$RESEARCH_PORT;Database=research_acceptance;Username=research_fixture;Password=disposable-ci-only;SSL Mode=Disable;Pooling=false"
export ASPNETCORE_ENVIRONMENT=Development
cp research/2a_synthetic/kde_comparison/t00t11-followup-20261123/UnitConversionService.cs 崴正/Services/UnitConversionService.cs
# Compile the authorization fixture only into the disposable runner checkout.
export RESEARCH_AUDIT_SUPERVISOR_TOKEN="$(python -c 'import secrets; print(secrets.token_hex(32))')"
export RESEARCH_AUDIT_READER_TOKEN="$(python -c 'import secrets; print(secrets.token_hex(32))')"
cp research/acceptance-ci/ResearchAuditAuthentication.cs 崴正/ResearchAuditAuthentication.cs
python - <<'PY'
from pathlib import Path

path = Path("崴正/Program.cs")
source = path.read_text(encoding="utf-8-sig")
for anchor, replacement in (
    ("builder.Services.AddControllers();", """builder.Services.AddControllers();
builder.Services.AddAuthentication(ResearchAuditAuthentication.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ResearchAuditAuthentication>(
        ResearchAuditAuthentication.SchemeName, _ => { });
builder.Services.AddAuthorization(options => options.AddPolicy("ResearchAuditSupervisor",
    policy => policy.RequireAuthenticatedUser().RequireRole("Supervisor")));"""),
    ('app.UseCors("Frontend");', """app.UseCors("Frontend");
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/audit-logs", StringComparison.OrdinalIgnoreCase))
    {
        await next(context);
        return;
    }

    var authorization = context.RequestServices.GetRequiredService<IAuthorizationService>();
    if ((await authorization.AuthorizeAsync(context.User, null, "ResearchAuditSupervisor")).Succeeded)
        await next(context);
    else if (context.User.Identity?.IsAuthenticated == true)
        await context.ForbidAsync();
    else
        await context.ChallengeAsync();
});"""),
):
    if source.count(anchor) != 1:
        raise SystemExit(f"Unexpected research overlay anchor: {anchor}")
    source = source.replace(anchor, replacement)
path.write_text("using Microsoft.AspNetCore.Authentication;\n"
                "using Microsoft.AspNetCore.Authorization;\n" + source, encoding="utf-8")
PY
dotnet run --project research/acceptance-ci/Fixture.csproj -c Release 2>&1 | tee evidence/api/fixture.log
dotnet 崴正/bin/Release/net10.0/崴正.dll --urls http://127.0.0.1:18081 > evidence/api/server.log 2>&1 &
server=$!
trap 'kill "$server" 2>/dev/null || true; wait "$server" 2>/dev/null || true; find evidence/api -type f ! -name SHA256SUMS -print0 | sort -z | xargs -0 -r sha256sum > evidence/api/SHA256SUMS' EXIT
for attempt in {1..40}; do
  if curl -fsS http://127.0.0.1:18081/swagger/v1/swagger.json -o /dev/null; then break; fi
  kill -0 "$server"
  sleep 1
done
if [[ "${RESEARCH_ESTIMATE_BENCHMARK:-}" == 1 ]]; then
  RESEARCH_SERVER_PID="$server" python research/acceptance-ci/benchmark-estimate-api.py | tee evidence/api/estimate.log
else
  python research/acceptance-ci/probe-api.py | tee evidence/api/http.jsonl
fi
