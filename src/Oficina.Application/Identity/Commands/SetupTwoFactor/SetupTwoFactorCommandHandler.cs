using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Sessions;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.SetupTwoFactor;

public sealed class SetupTwoFactorCommandHandler(IUserRepository users, ICurrentUser currentUser, ISecretProtector protector, IUnitOfWork unitOfWork)
    : IRequestHandler<SetupTwoFactorCommand, Result<TwoFactorSetupDto>>
{
    public const string Issuer = "Oficina";

    public async Task<Result<TwoFactorSetupDto>> Handle(SetupTwoFactorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || await users.GetByIdAsync(userId, cancellationToken) is not { } user)
        {
            return CommonErrors.Unauthorized;
        }

        var secret = Totp.NewSecret();
        user.PrepareTwoFactor(protector.Protect(secret));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new TwoFactorSetupDto(secret, Totp.Uri(Issuer, user.Email.Value, secret));
    }
}
