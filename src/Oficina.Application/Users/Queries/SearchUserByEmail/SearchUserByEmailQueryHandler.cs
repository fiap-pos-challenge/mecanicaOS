using MediatR;
using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Errors;

namespace Oficina.Application.Users.Queries.SearchUserByEmail;

public sealed class SearchUserByEmailQueryHandler(IUserQueries queries) : IRequestHandler<SearchUserByEmailQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(SearchUserByEmailQuery query, CancellationToken cancellationToken)
    {
        var email = Email.Create(query.Email);
        if (email.Error is { } error)
        {
            return error;
        }

        if (await queries.GetByEmailAsync(email.Value, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        return user;
    }
}
