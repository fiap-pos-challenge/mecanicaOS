using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Domain.Customers.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Customer?> GetByDocumentAsync(Document document, CancellationToken cancellationToken);

    void Add(Customer customer);

    void Remove(Customer customer);
}
