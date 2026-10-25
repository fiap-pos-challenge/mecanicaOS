using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Settings.Dtos;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Commands.SaveWorkshopSettings;

public sealed class SaveWorkshopSettingsCommandHandler(
    SettingsStore store,
    SettingsAudit audit,
    ReauthenticationGuard guard,
    ISettingsQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<SaveWorkshopSettingsCommand, Result<WorkshopSettings>>
{
    public async Task<Result<WorkshopSettings>> Handle(SaveWorkshopSettingsCommand command, CancellationToken cancellationToken)
    {
        var input = command.Input;
        if (Validate(input) is { } invalid)
        {
            return invalid;
        }

        if ((await guard.ConsumeAsync(command.Reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        var address = input.Address with
        {
            ZipCode = SettingsRules.Digits(input.Address.ZipCode),
            Street = input.Address.Street.Trim(),
            Number = input.Address.Number.Trim(),
            District = input.Address.District.Trim(),
            City = input.Address.City.Trim(),
            State = input.Address.State.Trim().ToUpperInvariant(),
        };
        var saved = current with
        {
            Name = input.Name.Trim(),
            Document = SettingsRules.Digits(input.Document),
            Address = address,
            Hours = SettingsRules.Weekdays.Select(day => input.Hours.First(hour => hour.Weekday == day)).ToList(),
        };
        await store.SaveAsync(SettingsKeys.Workshop, saved, cancellationToken);
        await audit.RecordAsync(SettingsAudit.Security, SettingsMessages.WorkshopChanged, saved.Name, string.Empty, cancellationToken);
        var withUsers = saved with { Plan = saved.Plan with { Users = await queries.ActiveUsersAsync(cancellationToken) } };
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : withUsers;
    }

    private static Error? Validate(WorkshopInput input)
    {
        var name = input.Name?.Trim() ?? string.Empty;
        if (name.Length is < 3 or > 120)
        {
            return SettingsErrors.Invalid("name", SettingsMessages.Name);
        }

        if (Document.Create(input.Document ?? string.Empty) is not { IsSuccess: true } document || document.Value.Type != DocumentType.Cnpj)
        {
            return SettingsErrors.Invalid("document", SettingsMessages.Document);
        }

        if (input.Address is not { } address || SettingsRules.Digits(address.ZipCode).Length != 8 || string.IsNullOrWhiteSpace(address.Street) || string.IsNullOrWhiteSpace(address.Number)
            || string.IsNullOrWhiteSpace(address.City) || (address.State?.Trim().Length ?? 0) != 2)
        {
            return SettingsErrors.Invalid("address", SettingsMessages.Address);
        }

        if (input.Hours is null || SettingsRules.Weekdays.Any(day => !input.Hours.Any(hour => hour.Weekday == day))
            || input.Hours.Any(hour => !hour.Closed && (!SettingsRules.IsTime(hour.Opens) || !SettingsRules.IsTime(hour.Closes) || string.CompareOrdinal(hour.Opens, hour.Closes) >= 0)))
        {
            return SettingsErrors.Invalid("hours", SettingsMessages.Hours);
        }

        return null;
    }
}
