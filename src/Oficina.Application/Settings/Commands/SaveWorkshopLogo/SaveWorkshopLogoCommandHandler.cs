using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Settings.Dtos;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Commands.SaveWorkshopLogo;

public sealed class SaveWorkshopLogoCommandHandler(SettingsStore store, SettingsAudit audit, ReauthenticationGuard guard, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<SaveWorkshopLogoCommand, Result<LogoDto>>
{
    public async Task<Result<LogoDto>> Handle(SaveWorkshopLogoCommand command, CancellationToken cancellationToken)
    {
        var input = command.Input;
        if (input.ContentType is not ("image/png" or "image/jpeg") || !input.DataUrl.StartsWith($"data:{input.ContentType};base64,", StringComparison.Ordinal))
        {
            return SettingsErrors.Invalid("logo", SettingsMessages.LogoType);
        }

        if ((input.DataUrl.Length - input.DataUrl.IndexOf(',', StringComparison.Ordinal) - 1) * 3 / 4 > SettingsRules.MaxLogoBytes)
        {
            return SettingsErrors.Invalid("logo", SettingsMessages.LogoSize);
        }

        if ((await guard.ConsumeAsync(command.Reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        await store.SaveAsync(SettingsKeys.Workshop, current with { LogoUrl = input.DataUrl }, cancellationToken);
        await audit.RecordAsync(SettingsAudit.Security, SettingsMessages.LogoChanged, current.Name, string.Empty, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : new LogoDto(input.DataUrl);
    }
}
