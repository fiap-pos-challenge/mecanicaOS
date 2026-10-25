using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Portal.Dtos;
using Oficina.Application.Quotes.Decisions;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.RejectQuoteFromPortal;

public sealed class RejectQuoteFromPortalCommandHandler(PortalAccess access, PortalOrderView view, QuoteDecider decider, IUnitOfWork unitOfWork)
    : IRequestHandler<RejectQuoteFromPortalCommand, Result<PortalOrderDto>>
{
    public async Task<Result<PortalOrderDto>> Handle(RejectQuoteFromPortalCommand command, CancellationToken cancellationToken)
    {
        var found = await access.SessionAsync(command.Session, cancellationToken);
        if (found.Error is { } sessionError)
        {
            return sessionError;
        }

        var estimate = found.Value;
        if ((await decider.RejectAsync(estimate, command.Reason ?? string.Empty, cancellationToken)).Error is { } rejectError)
        {
            return PortalAccess.ForPortal(rejectError);
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await view.ViewAsync(estimate, cancellationToken);
    }
}
