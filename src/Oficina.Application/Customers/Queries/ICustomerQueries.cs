using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Customers.Queries;

public interface ICustomerQueries
{
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CustomerDto?> GetByDocumentAsync(Document document, CancellationToken cancellationToken);

    Task<PagedResult<CustomerDto>> ListAsync(PageRequest page, CancellationToken cancellationToken);
}
