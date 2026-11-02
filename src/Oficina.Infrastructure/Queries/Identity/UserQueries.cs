using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Application.Users.Queries;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Identity;

public sealed class UserQueries(OficinaDbContext context) : IUserQueries
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        return user?.ToDto();
    }

    public async Task<UserDto?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
        return user?.ToDto();
    }

    public async Task<PagedResult<UserDto>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var users = context.Users.AsNoTracking();
        var totalItems = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(items.Select(user => user.ToDto()).ToList(), page.Page, page.PageSize, totalItems);
    }
}
