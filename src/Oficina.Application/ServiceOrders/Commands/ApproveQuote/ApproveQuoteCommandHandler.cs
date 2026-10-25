using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Quotes.Decisions;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.ApproveQuote;

public sealed class ApproveQuoteCommandHandler(
    IServiceOrderRepository serviceOrders,
    QuoteDecider decider,
    IServiceOrderQueries queries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveQuoteCommand, Result<ServiceOrderDetailDto>>
{
    public async Task<Result<ServiceOrderDetailDto>> Handle(ApproveQuoteCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.ServiceOrderId, cancellationToken) is not { } serviceOrder)
        {
            return ServiceOrderErrors.NotFound;
        }

        var approved = await decider.ApproveServiceOrderAsync(serviceOrder, currentUser.UserId, cancellationToken);
        if (approved.Error is { } approvalError)
        {
            return approvalError;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        if (await queries.GetByIdAsync(serviceOrder.Id, cancellationToken) is not { } detail)
        {
            return ServiceOrderErrors.NotFound;
        }

        return detail;
    }
}
