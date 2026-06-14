using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;

namespace Talabat.Tests.Integration;

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var email = Request.Headers.TryGetValue("X-Test-Email", out var values) && !StringValues.IsNullOrEmpty(values)
            ? values.ToString()
            : "buyer@test.com";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "integration-user"),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email)
        };

        if (Request.Headers.TryGetValue("X-Test-Roles", out var rolesHeader) && !StringValues.IsNullOrEmpty(rolesHeader))
        {
            var roles = rolesHeader
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
