using System.Text.Json;
using System.Text.RegularExpressions;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations;

public static partial class AutomationDefinitions
{
    public const int MaxItems = 5;
    public const int MaxMessage = 500;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] Placeholders = ["cliente", "os", "veiculo", "valor", "link"];
    private static readonly string[] OrderStatuses = ["Received", "InDiagnosis", "WaitingApproval", "InProgress", "Completed", "Delivered"];
    private static readonly string[] QuoteStatuses = ["Pending", "Approved", "Rejected"];
    private static readonly IReadOnlyDictionary<string, string[]> TriggerFields = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [AutomationTriggers.QuoteSent] = ["quoteStatus", "amount", "customerConsent"],
        [AutomationTriggers.ServiceOrderStatusChanged] = ["orderStatus", "customerConsent"],
        [AutomationTriggers.StockBelowMinimum] = ["partStock"],
        [AutomationTriggers.ServiceOrderDelivered] = ["amount", "customerConsent"],
        [AutomationTriggers.VehicleMaintenanceDue] = ["customerConsent"],
        [AutomationTriggers.PaymentOverdue] = ["amount", "daysOverdue", "customerConsent"],
        [AutomationTriggers.PaymentReceived] = ["amount"],
    };

    public static RuleDefinitionDto Read(string json) => JsonSerializer.Deserialize<RuleDefinitionDto>(json, Json)!;

    public static string Write(RuleDefinitionDto definition) => JsonSerializer.Serialize(Normalize(definition), Json);

    public static TimeSpan Wait(RuleWaitDto? wait) => wait is null ? TimeSpan.Zero : wait.Unit switch
    {
        "minutes" => TimeSpan.FromMinutes(wait.Amount),
        "hours" => TimeSpan.FromHours(wait.Amount),
        _ => TimeSpan.FromDays(wait.Amount),
    };

    public static Error? Check(RuleDefinitionDto? definition)
    {
        if (definition is null || definition.Name is null || definition.Actions is null || !AutomationTriggers.All.Contains(definition.Trigger)
            || definition.Actions.Any(action => action is null || !AutomationActions.All.Contains(action.Kind))
            || (definition.Conditions ?? []).Any(condition => condition is null || condition.Field is null || condition.Operator is null || condition.Value is null)
            || (definition.Wait is { } wait && wait.Unit is not ("minutes" or "hours" or "days")))
        {
            return AutomationErrors.InvalidDefinition;
        }

        var name = definition.Name.Trim();
        var conditions = definition.Conditions ?? [];
        var message = name.Length is < 3 or > 80 ? AutomationMessages.Name
            : definition.Actions.Count == 0 ? AutomationMessages.NoActions
            : conditions.Count > MaxItems ? AutomationMessages.TooManyConditions
            : definition.Actions.Count > MaxItems ? AutomationMessages.TooManyActions
            : conditions.Any(condition => !TriggerFields[definition.Trigger].Contains(condition.Field)) ? AutomationMessages.ConditionTrigger
            : conditions.Any(condition => !Operators(condition.Field).Contains(condition.Operator)) ? AutomationMessages.Operator
            : conditions.Any(condition => !ValidValue(condition)) ? AutomationMessages.Value
            : definition.Wait is { } pause && (pause.Amount < 1 || pause.Amount > MaxWait(pause.Unit)) ? AutomationMessages.Wait
            : definition.Actions.Any(action => (action.Kind == AutomationActions.CreatePurchaseOrder && definition.Trigger != AutomationTriggers.StockBelowMinimum)
                || (action.Kind == AutomationActions.RegisterPayment && definition.Trigger != AutomationTriggers.PaymentReceived)) ? AutomationMessages.ActionTrigger
            : definition.Actions.Any(action => AutomationActions.NeedsMessage(action.Kind) && string.IsNullOrWhiteSpace(action.Message)) ? AutomationMessages.Message
            : definition.Actions.Any(action => AutomationActions.NeedsMessage(action.Kind) && action.Message!.Trim().Length > MaxMessage) ? AutomationMessages.MessageLength
            : definition.Actions.Any(action => AutomationActions.NeedsMessage(action.Kind) && PlaceholderPattern().Matches(action.Message!).Any(match => !Placeholders.Contains(match.Groups[1].Value))) ? AutomationMessages.Placeholders
            : null;
        return message is null ? null : AutomationErrors.Invalid(message);
    }

    public static bool Matches(RuleConditionDto condition, AutomationContextValues context)
    {
        if (condition.Field is "orderStatus" or "quoteStatus" or "customerConsent")
        {
            var text = condition.Field switch
            {
                "orderStatus" => context.OrderStatus,
                "quoteStatus" => context.QuoteStatus,
                _ => context.HasConsent ? "true" : "false",
            };
            return text is not null && (condition.Operator == "notEquals" ? text != condition.Value : text == condition.Value);
        }

        decimal? actual = condition.Field switch { "amount" => context.Amount, "daysOverdue" => context.DaysOverdue, _ => context.PartStock };
        var expected = decimal.Parse(condition.Value.Trim().Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
        return actual is { } value && condition.Operator switch { "greaterThan" => value > expected, "lessThan" => value < expected, _ => value == expected };
    }

    public static string Render(string message, AutomationContextValues context) => PlaceholderPattern().Replace(message, match => match.Groups[1].Value switch
    {
        "cliente" => context.CustomerName?.Split(' ')[0] ?? string.Empty,
        "os" => context.Number?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        "veiculo" => context.Vehicle ?? string.Empty,
        "valor" => context.Amount?.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty,
        _ => context.Link ?? string.Empty,
    });

    [GeneratedRegex(@"\{([^{}]*)\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"^\d+([.,]\d{1,2})?$")]
    private static partial Regex NumberPattern();

    private static int MaxWait(string unit) => unit switch { "minutes" => 60 * 24 * 60, "hours" => 24 * 60, _ => 60 };

    private static RuleDefinitionDto Normalize(RuleDefinitionDto definition) => definition with
    {
        Name = definition.Name.Trim(),
        Conditions = definition.Conditions ?? [],
        Actions = definition.Actions.Select(action => action with { Message = action.Message?.Trim() }).ToList(),
    };

    private static string[] Operators(string field) => field switch
    {
        "orderStatus" or "quoteStatus" => ["equals", "notEquals"],
        "customerConsent" => ["equals"],
        _ => ["equals", "greaterThan", "lessThan"],
    };

    private static bool ValidValue(RuleConditionDto condition) => condition.Field switch
    {
        "orderStatus" => OrderStatuses.Contains(condition.Value),
        "quoteStatus" => QuoteStatuses.Contains(condition.Value),
        "customerConsent" => condition.Value is "true" or "false",
        _ => NumberPattern().IsMatch(condition.Value.Trim()),
    };
}
