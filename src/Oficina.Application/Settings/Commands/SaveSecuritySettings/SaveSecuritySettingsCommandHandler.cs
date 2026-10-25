using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Commands.SaveSecuritySettings;

public sealed class SaveSecuritySettingsCommandHandler(SettingsStore store, SettingsAudit audit, ReauthenticationGuard guard, IUnitOfWork unitOfWork)
    : IRequestHandler<SaveSecuritySettingsCommand, Result<SecuritySettings>>
{
    public async Task<Result<SecuritySettings>> Handle(SaveSecuritySettingsCommand command, CancellationToken cancellationToken)
    {
        var input = command.Input;
        if (input.IdleMinutes is < 5 or > 60)
        {
            return SettingsErrors.Invalid("idleMinutes", SettingsMessages.IdleMinutes);
        }

        if (input.LockoutAttempts is < 3 or > 10)
        {
            return SettingsErrors.Invalid("lockoutAttempts", SettingsMessages.LockoutAttempts);
        }

        if (input.LockoutMinutes is < 5 or > 120)
        {
            return SettingsErrors.Invalid("lockoutMinutes", SettingsMessages.LockoutMinutes);
        }

        if ((await guard.ConsumeAsync(command.Reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        await store.SaveAsync(SettingsKeys.Security, input, cancellationToken);
        await audit.RecordAsync(SettingsAudit.Security, SettingsMessages.SecurityChanged, $"{input.IdleMinutes} min de inatividade",
            input.RequireTwoFactorForAll ? SettingsMessages.TwoFactorRequired : SettingsMessages.TwoFactorOptional, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : input;
    }
}
