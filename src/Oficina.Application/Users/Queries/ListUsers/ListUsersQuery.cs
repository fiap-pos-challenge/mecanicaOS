using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Queries.ListUsers;

public sealed record ListUsersQuery(int Page, int PageSize) : IRequest<Result<PagedResult<UserDto>>>;
