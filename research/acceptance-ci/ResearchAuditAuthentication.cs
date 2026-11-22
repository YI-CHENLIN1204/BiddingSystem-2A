using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class ResearchAuditAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "ResearchAudit";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authorization))
            return Task.FromResult(AuthenticateResult.NoResult());

        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Invalid research credential."));

        var token = authorization[prefix.Length..];
        var role = Matches(token, "RESEARCH_AUDIT_SUPERVISOR_TOKEN") ? "Supervisor"
            : Matches(token, "RESEARCH_AUDIT_READER_TOKEN") ? "Reader" : null;
        if (role is null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid research credential."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "research-" + role), new Claim(ClaimTypes.Role, role)],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    private static bool Matches(string supplied, string variable)
    {
        var expected = Environment.GetEnvironmentVariable(variable);
        return !string.IsNullOrEmpty(expected) && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected));
    }
}
