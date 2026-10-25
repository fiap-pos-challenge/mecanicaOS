using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Users.Queries.ListUsers;

public sealed record ListUsersQuery(int Page, int PageSize, Role? Role = null, UserStatus? Status = null) : IRequest<Result<PagedResult<UserDto>>>;
