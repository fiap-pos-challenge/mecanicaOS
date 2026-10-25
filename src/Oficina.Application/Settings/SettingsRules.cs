using System.Text.RegularExpressions;

namespace Oficina.Application.Settings;

public static partial class SettingsRules
{
    public const int MaxLogoBytes = 512 * 1024;
    public const int MaxTemplateLength = 1000;
    public static readonly IReadOnlyList<string> Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
    public static readonly IReadOnlyList<string> PaymentMethods = ["Pix", "Credit", "Debit", "Cash", "Boleto"];
    public static readonly IReadOnlyList<string> Roles = ["Admin", "Manager", "Attendant", "Mechanic"];
    public static readonly IReadOnlyList<string> MessageKinds = ["QuoteSent", "StatusChanged", "QuoteReminder", "Receipt"];
    public static readonly IReadOnlyList<string> Variables = ["cliente", "veiculo", "placa", "total", "link", "prazo"];
    public static readonly IReadOnlyList<string> Conditions = ["LoyalCustomer", "FirstService", "Fleet"];
    public static readonly IReadOnlyList<string> AuditAreas = ["discounts", "payments", "security", "customers", "stock"];

    public static bool IsTime(string? value) => value is not null && TimePattern().IsMatch(value);

    public static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    public static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static IEnumerable<string> VariablesIn(string body) => VariablePattern().Matches(body).Select(match => match.Groups[1].Value);

    [GeneratedRegex(@"\{([^{}]*)\}")]
    private static partial Regex VariablePattern();

    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    private static partial Regex TimePattern();
}
