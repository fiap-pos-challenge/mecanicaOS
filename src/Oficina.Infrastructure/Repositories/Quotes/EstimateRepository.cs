using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Quotes;

public sealed class EstimateRepository(OficinaDbContext context) : IEstimateRepository
{
    public Task<Estimate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Estimates.SingleOrDefaultAsync(estimate => estimate.Id == id, cancellationToken);

    public Task<Estimate?> GetLatestByNumberAsync(long number, CancellationToken cancellationToken) =>
        context.Estimates.Where(estimate => estimate.Number == number).OrderByDescending(estimate => estimate.Version).FirstOrDefaultAsync(cancellationToken);

    public Task<Estimate?> GetLatestByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
        context.Estimates.Where(estimate => estimate.ServiceOrderId == serviceOrderId).OrderByDescending(estimate => estimate.Version).FirstOrDefaultAsync(cancellationToken);

    public Task<Estimate?> GetByDeliveryTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Estimates.FirstOrDefaultAsync(estimate => estimate.DeliveryTokenHash == tokenHash, cancellationToken);

    public Task<bool> ExistsForServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
        context.Estimates.AnyAsync(estimate => estimate.ServiceOrderId == serviceOrderId, cancellationToken);

    public async Task<long> NextNumberAsync(CancellationToken cancellationToken) =>
        (await context.Estimates.IgnoreQueryFilters().MaxAsync(estimate => (long?)estimate.Number, cancellationToken) ?? 0) + 1;

    public void Add(Estimate estimate) => context.Estimates.Add(estimate);
}
