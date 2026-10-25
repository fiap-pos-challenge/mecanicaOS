using MediatR;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;
