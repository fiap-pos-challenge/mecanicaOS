namespace Oficina.Application.Settings.Models;

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
