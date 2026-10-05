using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(IUserRepository users, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.Error is { } invalidEmail)
        {
            return invalidEmail;
        }

        if (await users.GetByEmailAsync(email.Value, cancellationToken) is not null)
        {
            return IdentityErrors.DuplicateEmail;
        }

        var user = User.Create(command.Name, email.Value, passwordHasher.Hash(command.Password), command.Role);
        if (user.Error is { } invalidUser)
        {
            return invalidUser;
        }

        users.Add(user.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return user.Value.ToDto();
    }
}
