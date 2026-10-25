namespace Oficina.Infrastructure.Payments;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    public string? PixWebhookSecret { get; set; }
}
