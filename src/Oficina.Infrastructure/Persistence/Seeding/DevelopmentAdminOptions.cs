namespace Oficina.Infrastructure.Persistence.Seeding;

public sealed class DevelopmentAdminOptions
{
    public const string SectionName = "DevelopmentAdmin";

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }
}
