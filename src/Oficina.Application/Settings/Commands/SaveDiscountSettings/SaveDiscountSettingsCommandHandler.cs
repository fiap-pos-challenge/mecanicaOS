using System.Globalization;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Commands.SaveDiscountSettings;

public sealed class SaveDiscountSettingsCommandHandler(SettingsStore store, SettingsAudit audit, ReauthenticationGuard guard, IUnitOfWork unitOfWork)
    : IRequestHandler<SaveDiscountSettingsCommand, Result<DiscountSettings>>
{
    public async Task<Result<DiscountSettings>> Handle(SaveDiscountSettingsCommand command, CancellationToken cancellationToken)
    {
        var rules = command.Input.Rules ?? [];
        var limits = command.Input.Limits ?? [];
        if (rules.Any(rule => string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Trim().Length > 80 || !SettingsRules.Conditions.Contains(rule.Condition)))
        {
            return SettingsErrors.Invalid("ruleName", SettingsMessages.RuleName);
        }

        if (rules.Any(rule => rule.Value <= 0 || (rule.Type == "Percent" && rule.Value > 100) || rule.Type is not ("Percent" or "Amount")))
        {
            return SettingsErrors.Invalid("ruleValue", SettingsMessages.RuleValue);
        }

        if (limits.Any(limit => !SettingsRules.Roles.Contains(limit.Role) || limit.Percent < 0 || limit.Percent > 100))
        {
            return SettingsErrors.Invalid("limits", SettingsMessages.Limits);
        }

        if ((await guard.ConsumeAsync(command.Reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Discounts, SettingsDefaults.Discounts, cancellationToken);
        var saved = new DiscountSettings(
            rules.Select(rule => rule with { Id = rule.Id == Guid.Empty ? Guid.CreateVersion7() : rule.Id, Name = rule.Name.Trim() }).ToList(),
            SettingsRules.Roles.Select(role => limits.FirstOrDefault(limit => limit.Role == role) ?? current.Limits.FirstOrDefault(limit => limit.Role == role) ?? new DiscountLimitSettings(role, 0)).ToList());
        await store.SaveAsync(SettingsKeys.Discounts, saved, cancellationToken);
        await audit.RecordAsync(SettingsAudit.Discounts, SettingsMessages.DiscountsChanged, $"{saved.Rules.Count} regras",
            string.Join(", ", saved.Limits.Select(limit => $"{limit.Role} {limit.Percent.ToString("0.##", CultureInfo.InvariantCulture)}%")), cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : saved;
    }
}
