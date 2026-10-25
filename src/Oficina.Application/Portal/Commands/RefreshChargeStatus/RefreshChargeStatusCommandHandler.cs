using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Portal.Repositories;

namespace Oficina.Application.Portal.Commands.RefreshChargeStatus;

public sealed class RefreshChargeStatusCommandHandler(PortalAccess access, IPortalRepository portal, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<RefreshChargeStatusCommand, Result<ChargeStatusDto>>
{
    public async Task<Result<ChargeStatusDto>> Handle(RefreshChargeStatusCommand command, CancellationToken cancellationToken)
    {
        var found = await access.SessionAsync(command.Session, cancellationToken);
        if (found.Error is { } sessionError)
        {
            return sessionError;
        }

        if (await portal.GetChargeAsync(command.PaymentId, cancellationToken) is not { } charge || charge.EstimateId != found.Value.Id)
        {
            return PortalErrors.PaymentNotFound;
        }

        var status = charge.Refresh(timeProvider.GetUtcNow());
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new ChargeStatusDto(status);
    }
}
