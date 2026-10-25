using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;

namespace Oficina.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserQueries queries) : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        return user;
    }
}
