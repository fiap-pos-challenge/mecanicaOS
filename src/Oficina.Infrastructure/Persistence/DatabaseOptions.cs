using System.ComponentModel.DataAnnotations;

namespace Oficina.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string? ConnectionString { get; set; }

    public bool ApplyMigrationsOnStartup { get; set; }
}
