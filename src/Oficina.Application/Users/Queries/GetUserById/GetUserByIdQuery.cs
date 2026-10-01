using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;
