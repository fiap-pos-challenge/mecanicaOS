namespace Oficina.Application.Quotes.Delivery;

public static class QuoteChannel
{
    public const string WhatsApp = "WhatsApp";
    public const string Email = "Email";
    public const string Link = "Link";

    public static readonly IReadOnlyList<string> All = [WhatsApp, Email, Link];
}
