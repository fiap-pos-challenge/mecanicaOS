using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Commands.SavePaymentSettings;

public sealed class SavePaymentSettingsCommandHandler(SettingsStore store, SettingsAudit audit, ReauthenticationGuard guard, IUnitOfWork unitOfWork)
    : IRequestHandler<SavePaymentSettingsCommand, Result<PaymentSettings>>
{
    public async Task<Result<PaymentSettings>> Handle(SavePaymentSettingsCommand command, CancellationToken cancellationToken)
    {
        var input = command.Input;
        var methods = (input.Methods ?? []).Distinct().ToList();
        if (methods.Count == 0 || methods.Any(method => !SettingsRules.PaymentMethods.Contains(method)))
        {
            return SettingsErrors.Invalid("methods", SettingsMessages.Methods);
        }

        if (input.MaxInstallments is < 1 or > 24 || input.InterestFreeInstallments < 1 || input.InterestFreeInstallments > input.MaxInstallments)
        {
            return SettingsErrors.Invalid("installments", SettingsMessages.Installments);
        }

        if (input.MonthlyInterestPercent is < 0 or > 15)
        {
            return SettingsErrors.Invalid("interest", SettingsMessages.Interest);
        }

        var fees = input.Fees ?? [];
        if (fees.Any(fee => !SettingsRules.PaymentMethods.Contains(fee.Method) || fee.FeePercent < 0 || fee.FeePercent > 20 || fee.SettlementDays is < 0 or > 60))
        {
            return SettingsErrors.Invalid("fees", SettingsMessages.Fees);
        }

        if ((await guard.ConsumeAsync(command.Reauthentication, cancellationToken)).Error is { } guardError)
        {
            return guardError;
        }

        var current = await store.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);
        var saved = new PaymentSettings(methods, input.MaxInstallments, input.InterestFreeInstallments, input.MonthlyInterestPercent, fees.ToList(), input.PixKey is null ? current.PixKey : SettingsRules.NullIfEmpty(input.PixKey));
        await store.SaveAsync(SettingsKeys.Payments, saved, cancellationToken);
        await audit.RecordAsync(SettingsAudit.Payments, SettingsMessages.PaymentsChanged, string.Join(", ", methods), string.Empty, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : saved;
    }
}
