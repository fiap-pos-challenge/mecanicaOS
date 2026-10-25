using Oficina.Domain.Catalog.Entities;

namespace Oficina.Domain.Catalog.Repositories;

public interface IPriceChangeRepository
{
    void Add(PriceChange? change);
}
