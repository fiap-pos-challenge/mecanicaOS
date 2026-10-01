using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Mappers;
using Oficina.Application.Customers.Queries;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Customers;

public sealed class CustomerQueries(OficinaDbContext context) : ICustomerQueries
{
    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.AsNoTracking().SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);
        return customer?.ToDto();
    }

    public async Task<CustomerDto?> GetByDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.AsNoTracking().SingleOrDefaultAsync(customer => customer.Document == document, cancellationToken);
        return customer?.ToDto();
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var customers = context.Customers.AsNoTracking();
        var totalItems = await customers.CountAsync(cancellationToken);
        var items = await customers
            .OrderBy(customer => customer.Name)
            .ThenBy(customer => customer.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items.Select(customer => customer.ToDto()).ToList(), page.Page, page.PageSize, totalItems);
    }
}
