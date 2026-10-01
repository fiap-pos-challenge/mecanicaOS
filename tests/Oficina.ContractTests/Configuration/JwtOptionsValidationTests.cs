using Microsoft.Extensions.Options;
using Oficina.ContractTests.Fixtures;

namespace Oficina.ContractTests.Configuration;

public sealed class JwtOptionsValidationTests
{
    [Theory]
    [InlineData("Jwt:Secret", "curta-demais")]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", "")]
    [InlineData("Jwt:ExpirationMinutes", "0")]
    public async Task Startup_WithInvalidJwtSettings_Fails(string key, string value)
    {
        await using var factory = new ContractApiFactory().WithWebHostBuilder(builder => builder.UseSetting(key, value));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.Services);

        Assert.Contains("JwtOptions", exception.OptionsName + exception.OptionsType.Name, StringComparison.Ordinal);
    }
}
