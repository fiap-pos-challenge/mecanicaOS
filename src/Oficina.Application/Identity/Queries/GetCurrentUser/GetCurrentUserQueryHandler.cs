using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Users.Queries;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser, IUserQueries queries)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId
            || await queries.GetByIdAsync(userId, cancellationToken) is not { IsActive: true } user)
        {
            return CommonErrors.Unauthorized;
        }

        return new CurrentUserDto(user.Id, user.Name, user.Email, user.Role);
    }
}
