namespace Oficina.Application.Quotes;

public static class EstimateLineSource
{
    public const string Manual = "Manual";
    public const string Ai = "Ai";

    public static string Normalize(string? source) => source == Ai ? Ai : Manual;
}
