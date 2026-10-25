using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Mappers;
using Oficina.Application.Customers.Queries;
using Oficina.Domain.Customers.Entities;
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

    public async Task<PagedResult<CustomerDto>> QueryAsync(CustomerFilter filter, PageRequest page, CancellationToken cancellationToken)
    {
        var customers = context.Customers.AsNoTracking();
        if (filter.Document is { } document)
        {
            customers = customers.Where(customer => customer.Document == document);
        }

        if (filter.Name is { } name)
        {
            customers = customers.Where(customer => customer.Name.Contains(name));
        }

        if (filter.DocumentType is { } documentType)
        {
            customers = customers.Where(customer => customer.DocumentType == documentType);
        }

        var totalItems = await customers.CountAsync(cancellationToken);
        var items = await Sort(customers, filter)
            .ThenBy(customer => customer.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items.Select(customer => customer.ToDto()).ToList(), page.Page, page.PageSize, totalItems);
    }

    private static IOrderedQueryable<Customer> Sort(IQueryable<Customer> customers, CustomerFilter filter) => (filter.Sort, filter.Direction) switch
    {
        (CustomerSort.CreatedAt, SortDirection.Asc) => customers.OrderBy(customer => customer.CreatedAt),
        (CustomerSort.CreatedAt, _) => customers.OrderByDescending(customer => customer.CreatedAt),
        (_, SortDirection.Desc) => customers.OrderByDescending(customer => customer.Name),
        _ => customers.OrderBy(customer => customer.Name),
    };
}
