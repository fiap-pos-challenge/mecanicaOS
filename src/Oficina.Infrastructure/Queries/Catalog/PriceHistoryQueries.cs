using Microsoft.EntityFrameworkCore;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries;
using Oficina.Domain.Catalog.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Catalog;

public sealed class PriceHistoryQueries(OficinaDbContext context) : IPriceHistoryQueries
{
    public const string UnknownAuthor = "Sistema";

    public async Task<IReadOnlyList<PriceChangeDto>> ListAsync(CatalogItemKind kind, Guid itemId, bool includeCost, CancellationToken cancellationToken)
    {
        var changes = await context.PriceChanges.AsNoTracking()
            .Where(change => change.Kind == kind && change.ItemId == itemId && (includeCost || change.Field != PriceField.Cost))
            .OrderByDescending(change => change.ChangedAt)
            .ThenByDescending(change => change.Id)
            .Select(change => new
            {
                change.Field,
                change.OldValue,
                change.NewValue,
                change.ChangedAt,
                Author = context.Users.IgnoreQueryFilters().Where(user => user.Id == change.ChangedBy).Select(user => user.Name).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return changes
            .Select(change => new PriceChangeDto(change.Field.ToString(), change.OldValue, change.NewValue, change.Author ?? UnknownAuthor, change.ChangedAt))
            .ToList();
    }
}
