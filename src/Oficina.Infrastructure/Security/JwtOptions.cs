using System.ComponentModel.DataAnnotations;

namespace Oficina.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSecretLength = 32;

    [Required]
    [MinLength(MinimumSecretLength)]
    public string? Secret { get; set; }

    [Required]
    public string? Issuer { get; set; }

    [Required]
    public string? Audience { get; set; }

    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; }
}
