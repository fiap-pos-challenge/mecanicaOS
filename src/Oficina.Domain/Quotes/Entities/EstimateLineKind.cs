namespace Oficina.Domain.Quotes.Entities;

public static class EstimateLineKind
{
    public const string Package = "Package";
    public const string Service = "Service";
    public const string Part = "Part";

    public static readonly IReadOnlyList<string> All = [Package, Service, Part];
}
