namespace Oficina.Domain.Quotes.Entities;

public static class DiscountMode
{
    public const string Automatic = "Automatic";
    public const string Manual = "Manual";
    public const string None = "None";

    public static string Normalize(string? mode) => mode is Manual or None ? mode : Automatic;
}
