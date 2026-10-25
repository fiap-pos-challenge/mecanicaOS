using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetDiscountSettings;

public sealed class GetDiscountSettingsQueryHandler(SettingsStore store) : IRequestHandler<GetDiscountSettingsQuery, Result<DiscountSettings>>
{
    public async Task<Result<DiscountSettings>> Handle(GetDiscountSettingsQuery query, CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Discounts, SettingsDefaults.Discounts, cancellationToken);
}
