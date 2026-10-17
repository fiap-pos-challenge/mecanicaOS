using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory;
using Oficina.Application.Notifications;
using Oficina.Domain.Automations;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations;

public sealed partial class AutomationService(
    IAutomationRepository automations,
    IAutomationQueries queries,
    INotificationPublisher notifications,
    PurchaseOrderDrafts purchaseOrders,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AutomationService> logger)
{
    public const int MaxItems = 5;
    public const int MaxMessage = 500;
    public static readonly string[] Triggers = ["QuoteSent", "ServiceOrderStatusChanged", "StockBelowMinimum", "ServiceOrderDelivered", "VehicleMaintenanceDue", "PaymentOverdue", "PaymentReceived"];
    public static readonly string[] Actions = ["SendWhatsApp", "SendEmail", "NotifyTeam", "CreatePurchaseOrder", "RegisterPayment"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] Placeholders = ["cliente", "os", "veiculo", "valor", "link"];
    private static readonly string[] OrderStatuses = ["Received", "InDiagnosis", "WaitingApproval", "InProgress", "Completed", "Delivered"];
    private static readonly string[] QuoteStatuses = ["Pending", "Approved", "Rejected"];
    private static readonly IReadOnlyDictionary<string, string[]> TriggerFields = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["QuoteSent"] = ["quoteStatus", "amount", "customerConsent"],
        ["ServiceOrderStatusChanged"] = ["orderStatus", "customerConsent"],
        ["StockBelowMinimum"] = ["partStock"],
        ["ServiceOrderDelivered"] = ["amount", "customerConsent"],
        ["VehicleMaintenanceDue"] = ["customerConsent"],
        ["PaymentOverdue"] = ["amount", "daysOverdue", "customerConsent"],
        ["PaymentReceived"] = ["amount"],
    };

    public async Task<Result<AutomationRuleListDto>> ListAsync(CancellationToken cancellationToken)
    {
        var stats = await queries.StatsAsync(timeProvider.GetUtcNow().AddDays(-1), cancellationToken);
        return new AutomationRuleListDto((await automations.RulesAsync(cancellationToken)).Select(rule => Dto(rule, stats)).ToList());
    }

    public async Task<Result<AutomationRuleDto>> CreateAsync(RuleDefinitionDto? definition, CancellationToken cancellationToken)
    {
        if (Check(definition) is { } error)
        {
            return error;
        }

        var rule = AutomationRule.Create(definition!.Trigger, JsonSerializer.Serialize(Normalize(definition), Json), timeProvider.GetUtcNow());
        automations.Add(rule);
        return await CommitAsync(rule, cancellationToken);
    }

    public async Task<Result<AutomationRuleDto>> UpdateAsync(Guid id, RuleDefinitionDto? definition, CancellationToken cancellationToken)
    {
        if (await automations.GetRuleAsync(id, cancellationToken) is not { } rule)
        {
            return AutomationErrors.NotFound;
        }

        if (Check(definition) is { } error)
        {
            return error;
        }

        rule.Update(definition!.Trigger, JsonSerializer.Serialize(Normalize(definition), Json));
        return await CommitAsync(rule, cancellationToken);
    }

    public async Task<Result<AutomationRuleDto>> SetStatusAsync(Guid id, bool? enabled, CancellationToken cancellationToken)
    {
        if (await automations.GetRuleAsync(id, cancellationToken) is not { } rule)
        {
            return AutomationErrors.NotFound;
        }

        if (enabled is not { } value)
        {
            return AutomationErrors.InvalidEnabled;
        }

        rule.SetEnabled(value);
        return await CommitAsync(rule, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await automations.GetRuleAsync(id, cancellationToken) is not { } rule)
        {
            return AutomationErrors.NotFound;
        }

        automations.Remove(rule);
        return await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<Result<QueueHealthListDto>> QueuesAsync(CancellationToken cancellationToken) =>
        new QueueHealthListDto(await queries.QueuesAsync(timeProvider.GetUtcNow().AddDays(-1), cancellationToken));

    public async Task<Result<QueueFailureListDto>> FailuresAsync(CancellationToken cancellationToken) =>
        new QueueFailureListDto(await queries.FailuresAsync(cancellationToken));

    public async Task<Result> RetryAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await automations.GetExecutionAsync(id, cancellationToken) is { Status: ExecutionStatus.Failed } execution)
        {
            execution.Retry(timeProvider.GetUtcNow());
            return await unitOfWork.CommitAsync(cancellationToken);
        }

        return await queries.RetryOutboxAsync(id, cancellationToken) ? Result.Success() : AutomationErrors.NotFound;
    }

    public async Task FireAsync(string trigger, Guid? subjectId, string dedupeKey, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var rule in await automations.EnabledAsync(trigger, cancellationToken))
        {
            if (await automations.ExecutionExistsAsync(rule.Id, dedupeKey, cancellationToken))
            {
                continue;
            }

            var definition = JsonSerializer.Deserialize<RuleDefinitionDto>(rule.DefinitionJson, Json)!;
            automations.Add(AutomationExecution.Schedule(rule.Id, trigger, subjectId, dedupeKey, now + Wait(definition.Wait), now));
        }
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var subject in await queries.ScanAsync(now, cancellationToken))
        {
            await FireAsync(subject.Trigger, subject.SubjectId, subject.DedupeKey, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        var due = await automations.DueAsync(timeProvider.GetUtcNow(), 50, cancellationToken);
        foreach (var execution in due)
        {
            await ExecuteAsync(execution, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return due.Count;
    }

    [GeneratedRegex(@"\{([^{}]*)\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"^\d+([.,]\d{1,2})?$")]
    private static partial Regex NumberPattern();

    [LoggerMessage(Level = LogLevel.Information, Message = "Automation queued a WhatsApp message for {Trigger}")]
    private static partial void LogWhatsApp(ILogger logger, string trigger);

    private static TimeSpan Wait(RuleWaitDto? wait) => wait is null ? TimeSpan.Zero : wait.Unit switch
    {
        "minutes" => TimeSpan.FromMinutes(wait.Amount),
        "hours" => TimeSpan.FromHours(wait.Amount),
        _ => TimeSpan.FromDays(wait.Amount),
    };

    private static RuleDefinitionDto Normalize(RuleDefinitionDto definition) => definition with
    {
        Name = definition.Name.Trim(),
        Conditions = definition.Conditions ?? [],
        Actions = definition.Actions.Select(action => action with { Message = action.Message?.Trim() }).ToList(),
    };

    private static Error? Check(RuleDefinitionDto? definition)
    {
        if (definition is null || definition.Name is null || definition.Actions is null || !Triggers.Contains(definition.Trigger)
            || definition.Actions.Any(action => action is null || !Actions.Contains(action.Kind))
            || (definition.Conditions ?? []).Any(condition => condition is null || condition.Field is null || condition.Operator is null || condition.Value is null)
            || (definition.Wait is { } wait && wait.Unit is not ("minutes" or "hours" or "days")))
        {
            return AutomationErrors.InvalidDefinition;
        }

        var name = definition.Name.Trim();
        var conditions = definition.Conditions ?? [];
        var message = name.Length is < 3 or > 80 ? "Dê um nome com 3 a 80 caracteres."
            : definition.Actions.Count == 0 ? "Adicione pelo menos uma ação."
            : conditions.Count > MaxItems ? "Use até 5 condições."
            : definition.Actions.Count > MaxItems ? "Use até 5 ações."
            : conditions.Any(condition => !TriggerFields[definition.Trigger].Contains(condition.Field)) ? "Esta condição não vale para o gatilho escolhido."
            : conditions.Any(condition => !Operators(condition.Field).Contains(condition.Operator)) ? "Operador inválido para a condição."
            : conditions.Any(condition => !ValidValue(condition)) ? "Valor inválido na condição."
            : definition.Wait is { } pause && (pause.Amount < 1 || pause.Amount > (pause.Unit switch { "minutes" => 60 * 24 * 60, "hours" => 24 * 60, _ => 60 })) ? "Tempo de espera inválido."
            : definition.Actions.Any(action => (action.Kind == "CreatePurchaseOrder" && definition.Trigger != "StockBelowMinimum") || (action.Kind == "RegisterPayment" && definition.Trigger != "PaymentReceived")) ? "Esta ação não vale para o gatilho escolhido."
            : definition.Actions.Any(action => NeedsMessage(action.Kind) && string.IsNullOrWhiteSpace(action.Message)) ? "Escreva a mensagem da ação."
            : definition.Actions.Any(action => NeedsMessage(action.Kind) && action.Message!.Trim().Length > MaxMessage) ? "A mensagem pode ter até 500 caracteres."
            : definition.Actions.Any(action => NeedsMessage(action.Kind) && PlaceholderPattern().Matches(action.Message!).Any(match => !Placeholders.Contains(match.Groups[1].Value))) ? "Use só {cliente}, {os}, {veiculo}, {valor} e {link}."
            : null;
        return message is null ? null : AutomationErrors.Invalid(message);
    }

    private static bool NeedsMessage(string kind) => kind is "SendWhatsApp" or "SendEmail" or "NotifyTeam";

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

    private static bool Matches(RuleConditionDto condition, AutomationContext context)
    {
        string? text = condition.Field switch
        {
            "orderStatus" => context.OrderStatus,
            "quoteStatus" => context.QuoteStatus,
            "customerConsent" => (context.ConsentEmail || context.ConsentWhatsApp) ? "true" : "false",
            _ => null,
        };
        if (condition.Field is "orderStatus" or "quoteStatus" or "customerConsent")
        {
            return text is not null && (condition.Operator == "notEquals" ? text != condition.Value : text == condition.Value);
        }

        decimal? actual = condition.Field switch { "amount" => context.Amount, "daysOverdue" => context.DaysOverdue, _ => context.PartStock };
        var expected = decimal.Parse(condition.Value.Trim().Replace(',', '.'), CultureInfo.InvariantCulture);
        return actual is { } value && condition.Operator switch { "greaterThan" => value > expected, "lessThan" => value < expected, _ => value == expected };
    }

    private static string Render(string message, AutomationContext context) => PlaceholderPattern().Replace(message, match => match.Groups[1].Value switch
    {
        "cliente" => context.CustomerName?.Split(' ')[0] ?? string.Empty,
        "os" => context.Number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        "veiculo" => context.Vehicle ?? string.Empty,
        "valor" => context.Amount?.ToString("C", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty,
        _ => context.Link ?? string.Empty,
    });

    private static AutomationRuleDto Dto(AutomationRule rule, IReadOnlyList<RuleStats> stats)
    {
        var stat = stats.FirstOrDefault(item => item.RuleId == rule.Id);
        return new AutomationRuleDto(rule.Id, rule.Enabled, JsonSerializer.Deserialize<RuleDefinitionDto>(rule.DefinitionJson, Json)!, stat?.Runs24h ?? 0, stat?.LastRunAt);
    }

    private async Task<Result<AutomationRuleDto>> CommitAsync(AutomationRule rule, CancellationToken cancellationToken)
    {
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } error)
        {
            return error;
        }

        return Dto(rule, await queries.StatsAsync(timeProvider.GetUtcNow().AddDays(-1), cancellationToken));
    }

    private async Task ExecuteAsync(AutomationExecution execution, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (await automations.GetRuleAsync(execution.RuleId, cancellationToken) is not { Enabled: true } rule)
        {
            execution.Finish(ExecutionStatus.Skipped, "Regra desligada ou removida", null, now);
            return;
        }

        var definition = JsonSerializer.Deserialize<RuleDefinitionDto>(rule.DefinitionJson, Json)!;
        if (await queries.ContextAsync(execution.Trigger, execution.SubjectId, cancellationToken) is not { } context)
        {
            execution.Finish(ExecutionStatus.Skipped, $"{definition.Name}: registro não encontrado", null, now);
            return;
        }

        if (!definition.Conditions.All(condition => Matches(condition, context)))
        {
            execution.Finish(ExecutionStatus.Skipped, $"{definition.Name}: condições não atendidas", null, now);
            return;
        }

        var done = new List<string>();
        foreach (var action in definition.Actions)
        {
            var outcome = await ActAsync(action, context, execution.Trigger, cancellationToken);
            if (outcome.Error is { } error)
            {
                execution.Finish(ExecutionStatus.Failed, $"{definition.Name}{(context.Number is { } number ? $" · OS {number}" : context.PartName is { } part ? $" · {part}" : string.Empty)}", error.Message, now);
                await notifications.PublishAsync("AutomationFailed", $"Falha na automação {definition.Name}", $"{error.Message} O item está na fila de erros.", "/automations", false, null, cancellationToken);
                return;
            }

            done.Add(outcome.Value);
        }

        execution.Finish(ExecutionStatus.Done, $"{definition.Name}: {string.Join("; ", done)}", null, now);
    }

    private async Task<Result<string>> ActAsync(RuleActionDto action, AutomationContext context, string trigger, CancellationToken cancellationToken)
    {
        switch (action.Kind)
        {
            case "SendEmail":
                if (!context.ConsentEmail || context.Email is null)
                {
                    return "e-mail não enviado: sem consentimento";
                }

                var sent = await emailSender.SendAsync(new EmailMessage(context.Email, context.Number is { } number ? $"OS nº {number}" : "Sua oficina", $"<p>{System.Net.WebUtility.HtmlEncode(Render(action.Message!, context))}</p>"), cancellationToken);
                return sent ? "e-mail enviado" : new Error("automations.emailFailed", "Não foi possível enviar o e-mail.", ErrorType.Unexpected);
            case "SendWhatsApp":
                if (!context.ConsentWhatsApp || context.Phone is null)
                {
                    return "WhatsApp não enviado: sem consentimento";
                }

                LogWhatsApp(logger, trigger);
                return "WhatsApp na fila";
            case "NotifyTeam":
                var type = trigger switch { "StockBelowMinimum" => "StockLow", "PaymentReceived" => "PaymentConfirmed", "QuoteSent" => "QuoteAnswered", _ => "ServiceOrderChanged" };
                await notifications.PublishAsync(type, Render(action.Message!, context), context.Number is { } os ? $"OS nº {os}" : context.PartName ?? string.Empty, context.ServiceOrderId is null ? "/inventory" : "/service-orders", false, null, cancellationToken);
                return "equipe avisada";
            case "CreatePurchaseOrder":
                if (context.PartId is not { } partId)
                {
                    return new Error("automations.noPart", "A regra não encontrou a peça.", ErrorType.BusinessRule);
                }

                var order = await purchaseOrders.CreateAsync(partId, cancellationToken);
                return order.Error is { } error ? error : $"pedido de compra nº {order.Value.Number} criado";
            default:
                return "pagamento registrado";
        }
    }
}
