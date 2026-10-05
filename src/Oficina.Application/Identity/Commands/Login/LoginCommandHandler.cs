using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.Login;

public sealed class LoginCommandHandler(IUserRepository users, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        var user = email.IsSuccess ? await users.GetByEmailAsync(email.Value, cancellationToken) : null;
        var passwordMatches = passwordHasher.Verify(command.Password, user?.PasswordHash ?? passwordHasher.DummyHash);
        if (user is not { CanLogin: true } || !passwordMatches)
        {
            return IdentityErrors.InvalidCredentials;
        }

        var token = tokenGenerator.Generate(user);
        return new LoginResponse(token.Value, token.ExpiresAt);
    }
}
