using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Catalog;

public sealed class PriceChangeRepository(OficinaDbContext context) : IPriceChangeRepository
{
    public void Add(PriceChange? change)
    {
        if (change is not null)
        {
            context.PriceChanges.Add(change);
        }
    }
}
