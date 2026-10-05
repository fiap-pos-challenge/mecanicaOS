using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Users.Queries.SearchUserByEmail;

public sealed record SearchUserByEmailQuery(string Email) : IRequest<Result<UserDto>>;
