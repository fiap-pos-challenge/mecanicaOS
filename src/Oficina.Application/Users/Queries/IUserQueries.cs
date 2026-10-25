using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Application.Users.Queries;

public interface IUserQueries
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserDto?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<PagedResult<UserDto>> ListAsync(PageRequest page, UserFilter filter, CancellationToken cancellationToken);
}
