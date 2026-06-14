using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Talabat.Core.Entities.Identity;
using Talabat.Service;
using Talabat.Tests.TestSupport;

namespace Talabat.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public async Task CreateTokenAsync_ShouldIncludeUserClaimsAndRoles()
    {
        var config = TestDataFactory.CreateConfiguration(new Dictionary<string, string>
        {
            ["JWT:Key"] = "this-is-a-very-long-test-key-for-jwt-signing-12345",
            ["JWT:ValidIssuer"] = "https://issuer.test",
            ["JWT:ValidAudience"] = "audience.test",
            ["JWT:DurationInDays"] = "2"
        });

        var tokenService = new TokenService(config);
        var user = new AppUser
        {
            Id = "user-1",
            DisplayName = "Ahmed Farouk",
            Email = "ahmed@test.com"
        };

        var userManager = TestDataFactory.CreateUserManagerMock(new List<string> { "Admin", "Manager" });

        var token = await tokenService.CreateTokenAsync(user, userManager.Object);

        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.GivenName && claim.Value == "Ahmed Farouk");
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Email && claim.Value == "ahmed@test.com");
        jwt.Claims.Count(claim => claim.Type == ClaimTypes.Role).Should().Be(2);
        jwt.Issuer.Should().Be("https://issuer.test");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("audience.test");
    }
}
