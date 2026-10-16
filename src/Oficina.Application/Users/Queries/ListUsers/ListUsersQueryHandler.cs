using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Queries.ListUsers;

public sealed class ListUsersQueryHandler(IUserQueries queries) : IRequestHandler<ListUsersQuery, Result<PagedResult<UserDto>>>
{
    public async Task<Result<PagedResult<UserDto>>> Handle(ListUsersQuery query, CancellationToken cancellationToken) =>
        await queries.ListAsync(new PageRequest(query.Page, query.PageSize), new UserFilter(query.Role, query.Status), cancellationToken);
}
