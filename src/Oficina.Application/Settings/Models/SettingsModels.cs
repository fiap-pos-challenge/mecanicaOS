namespace Oficina.Application.Settings.Models;

public sealed record WorkshopAddressSettings(string ZipCode, string Street, string Number, string District, string City, string State);

public sealed record BusinessHoursSettings(string Weekday, bool Closed, string Opens, string Closes);

public sealed record WorkshopPlanSettings(string Name, DateTimeOffset RenewsAt, int Users, int MaxUsers);

public sealed record WorkshopSettings(string Name, string Document, WorkshopAddressSettings Address, string? LogoUrl, IReadOnlyList<BusinessHoursSettings> Hours, WorkshopPlanSettings Plan);

public sealed record DiscountRuleSettings(Guid Id, string Name, string Type, decimal Value, string Condition, bool IsActive);

public sealed record DiscountLimitSettings(string Role, decimal Percent);

public sealed record DiscountSettings(IReadOnlyList<DiscountRuleSettings> Rules, IReadOnlyList<DiscountLimitSettings> Limits);

public sealed record PaymentFeeSettings(string Method, decimal FeePercent, int SettlementDays);

public sealed record PaymentSettings(
    IReadOnlyList<string> Methods,
    int MaxInstallments,
    int InterestFreeInstallments,
    decimal MonthlyInterestPercent,
    IReadOnlyList<PaymentFeeSettings> Fees,
    string? PixKey);

public sealed record MessageTemplateSettings(string Kind, string Channel, string Body);

public sealed record SendWindowSettings(string From, string To, IReadOnlyList<string> Weekdays);

public sealed record MessageSettings(IReadOnlyList<MessageTemplateSettings> Templates, SendWindowSettings Window);

public sealed record SecuritySettings(bool RequireTwoFactorForAll, int IdleMinutes, int LockoutAttempts, int LockoutMinutes);

public sealed record FiscalIntegrationSettings(string Kind, string Status, string? Provider);

public sealed record FiscalSettings(IReadOnlyList<FiscalIntegrationSettings> Integrations);

public static class SettingsKeys
{
    public const string Workshop = "workshop";
    public const string Discounts = "discounts";
    public const string Payments = "payments";
    public const string Messages = "messages";
    public const string Security = "security";
    public const string Fiscal = "fiscal";
}

public static class SettingsDefaults
{
    public static readonly IReadOnlyList<int> ExpirationDays = [1, 3, 7, 15];

    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    public static WorkshopSettings Workshop(DateTimeOffset now) => new(
        "Oficina",
        string.Empty,
        new WorkshopAddressSettings(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
        null,
        Weekdays.Select(day => new BusinessHoursSettings(day, day == "Sunday", "08:00", day == "Saturday" ? "12:00" : "18:00")).ToList(),
        new WorkshopPlanSettings("Essencial", now.AddYears(1), 0, 15));

    public static readonly DiscountSettings Discounts = new(
        [],
        [new("Admin", 30m), new("Manager", 20m), new("Attendant", 10m), new("Mechanic", 0m)]);

    public static readonly PaymentSettings Payments = new(
        ["Pix", "Credit", "Debit", "Cash"],
        6,
        3,
        1.99m,
        [new("Pix", 0m, 0), new("Credit", 3.49m, 30), new("Debit", 1.89m, 1), new("Cash", 0m, 0), new("Boleto", 2.5m, 2)],
        null);

    public static readonly MessageSettings Messages = new(
        [
            new("QuoteSent", "WhatsApp", "Olá, {cliente}! O orçamento do seu {veiculo} ({placa}) está pronto: {total}. Veja e aprove pelo link em até {prazo} dias: {link}"),
            new("QuoteSent", "Email", "Olá, {cliente}! O orçamento do seu {veiculo} ({placa}) está pronto: {total}. Veja e aprove pelo link em até {prazo} dias: {link}"),
            new("StatusChanged", "WhatsApp", "Olá, {cliente}! Seu {veiculo} ({placa}) mudou de etapa. Acompanhe: {link}"),
            new("QuoteReminder", "WhatsApp", "Olá, {cliente}! O orçamento do seu {veiculo} ainda aguarda sua resposta: {link}"),
            new("Receipt", "Email", "Olá, {cliente}! Obrigado pela preferência. Seu recibo: {link}"),
        ],
        new SendWindowSettings("08:00", "20:00", ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]));

    public static readonly SecuritySettings Security = new(false, 30, 5, 15);

    public static readonly FiscalSettings Fiscal = new(
        [new("NFSe", "Planned", null), new("NFe", "NotConfigured", null), new("NFCe", "NotConfigured", null)]);
}
