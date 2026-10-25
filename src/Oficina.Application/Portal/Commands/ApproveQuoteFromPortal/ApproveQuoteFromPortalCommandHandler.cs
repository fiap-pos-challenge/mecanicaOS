using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Portal.Dtos;
using Oficina.Application.Quotes.Decisions;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.ApproveQuoteFromPortal;

public sealed class ApproveQuoteFromPortalCommandHandler(PortalAccess access, PortalOrderView view, QuoteDecider decider, IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveQuoteFromPortalCommand, Result<PortalOrderDto>>
{
    public async Task<Result<PortalOrderDto>> Handle(ApproveQuoteFromPortalCommand command, CancellationToken cancellationToken)
    {
        var found = await access.SessionAsync(command.Session, cancellationToken);
        if (found.Error is { } sessionError)
        {
            return sessionError;
        }

        var estimate = found.Value;
        if ((await decider.ApproveAsync(estimate, command.ItemIds.Distinct().ToList(), cancellationToken)).Error is { } approveError)
        {
            return PortalAccess.ForPortal(approveError);
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await view.ViewAsync(estimate, cancellationToken);
    }
}
