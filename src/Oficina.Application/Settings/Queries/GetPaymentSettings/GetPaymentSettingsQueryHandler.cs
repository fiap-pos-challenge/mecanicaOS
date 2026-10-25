using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetPaymentSettings;

public sealed class GetPaymentSettingsQueryHandler(SettingsStore store) : IRequestHandler<GetPaymentSettingsQuery, Result<PaymentSettings>>
{
    public async Task<Result<PaymentSettings>> Handle(GetPaymentSettingsQuery query, CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);
}
