namespace Oficina.Infrastructure.Security;

public sealed class DataProtectionOptions
{
    public const string SectionName = "DataProtection";
    public const string ApplicationName = "mecanicaOS";

    public string? KeysPath { get; set; }
}
