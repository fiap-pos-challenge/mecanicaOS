using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Security;

namespace Oficina.FunctionalTests.Security;

public sealed class JwtTokenGeneratorTests
{
    private readonly FakeTimeProvider _timeProvider = new(TestData.Today);
    private readonly JwtOptions _options = new()
    {
        Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
        Issuer = "oficina-api",
        Audience = "oficina-clients",
        ExpirationMinutes = 60,
    };

    [Fact]
    public async Task Generate_CreatesASignedTokenForTheIssuerAndAudience()
    {
        var user = TestData.User(role: Role.Mechanic);

        var token = new JwtTokenGenerator(Options.Create(_options), _timeProvider).Generate(user);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret!)),
            ValidateLifetime = false,
        });
        Assert.True(validation.IsValid);
        Assert.Equal(user.Id.ToString(), validation.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("Mechanic", validation.Claims[JwtTokenGenerator.RoleClaimType]);
    }

    [Fact]
    public void Generate_ExpiresAfterTheConfiguredMinutes()
    {
        var token = new JwtTokenGenerator(Options.Create(_options), _timeProvider).Generate(TestData.User());

        Assert.Equal(TestData.Today.AddMinutes(60), token.ExpiresAt);
        Assert.Equal(TestData.Today.AddMinutes(60).UtcDateTime, new JsonWebToken(token.Value).ValidTo);
    }

    [Fact]
    public void Generate_DoesNotPutPersonalDataInTheToken()
    {
        var user = TestData.User();

        var token = new JsonWebToken(new JwtTokenGenerator(Options.Create(_options), _timeProvider).Generate(user).Value);

        Assert.DoesNotContain(token.Claims, claim => claim.Value == user.Email.Value || claim.Value == user.Name);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Alg);
    }
}
