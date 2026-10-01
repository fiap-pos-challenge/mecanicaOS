using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Customers;

public sealed class CustomerRepository(OficinaDbContext context) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<Customer?> GetByDocumentAsync(Document document, CancellationToken cancellationToken) =>
        context.Customers.SingleOrDefaultAsync(customer => customer.Document == document, cancellationToken);

    public void Add(Customer customer) => context.Customers.Add(customer);

    public void Remove(Customer customer) => context.Customers.Remove(customer);
}
