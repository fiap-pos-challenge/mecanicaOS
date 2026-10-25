using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Commands.SaveMessageSettings;

public sealed class SaveMessageSettingsCommandHandler(SettingsStore store, SettingsAudit audit, ReauthenticationGuard guard, IUnitOfWork unitOfWork)
    : IRequestHandler<SaveMessageSettingsCommand, Result<MessageSettings>>
{
    public async Task<Result<MessageSettings>> Handle(SaveMessageSettingsCommand command, CancellationToken cancellationToken)
    {
        var templates = command.Input.Templates ?? [];
        if (templates.Any(template => !SettingsRules.MessageKinds.Contains(template.Kind) || template.Channel is not ("WhatsApp" or "Email")))
        {
            return SettingsErrors.Invalid("templates", SettingsMessages.Template);
        }

        if (templates.Any(template => string.IsNullOrWhiteSpace(template.Body) || template.Body.Length > SettingsRules.MaxTemplateLength))
        {
            return SettingsErrors.Invalid("templates", SettingsMessages.TemplateLength);
        }

        if (templates.Any(template => SettingsRules.VariablesIn(template.Body).Any(variable => !SettingsRules.Variables.Contains(variable))))
        {
            return SettingsErrors.Invalid("templates", SettingsMessages.TemplateVariables);
        }

        if (command.Input.Window is not { } window || !SettingsRules.IsTime(window.From) || !SettingsRules.IsTime(window.To) || string.CompareOrdinal(window.From, window.To) >= 0
            || (window.Weekdays ?? []).Any(day => !SettingsRules.Weekdays.Contains(day)))
        {
            return SettingsErrors.Invalid("window", SettingsMessages.Window);
        }

        if ((await guard.ConsumeAsync(command.Reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var saved = new MessageSettings(templates.Select(template => template with { Body = template.Body.Trim() }).ToList(),
            window with { Weekdays = SettingsRules.Weekdays.Where(day => (window.Weekdays ?? []).Contains(day)).ToList() });
        await store.SaveAsync(SettingsKeys.Messages, saved, cancellationToken);
        await audit.RecordAsync(SettingsAudit.Security, SettingsMessages.MessagesChanged, $"{saved.Templates.Count} modelos", string.Empty, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : saved;
    }
}
