using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Quotes.Decisions;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.ServiceOrders.Commands.ApproveQuoteByToken;

public sealed class ApproveQuoteByTokenCommandHandler(
    IServiceOrderRepository serviceOrders,
    QuoteDecider decider,
    IServiceOrderQueries queries,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveQuoteByTokenCommand, Result<PublicServiceOrderDto>>
{
    public async Task<Result<PublicServiceOrderDto>> Handle(ApproveQuoteByTokenCommand command, CancellationToken cancellationToken)
    {
        var token = ApprovalToken.FromRawToken(command.Token);
        if (token.Error is { } tokenError)
        {
            return tokenError;
        }

        if (await serviceOrders.GetByApprovalTokenAsync(token.Value, cancellationToken) is not { } serviceOrder)
        {
            return ServiceOrderErrors.NotFound;
        }

        var approved = await decider.ApproveServiceOrderAsync(serviceOrder, null, cancellationToken);
        if (approved.Error is { } approvalError)
        {
            return approvalError;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        if (await queries.GetPublicByIdAsync(serviceOrder.Id, cancellationToken) is not { } publicServiceOrder)
        {
            return ServiceOrderErrors.NotFound;
        }

        return publicServiceOrder;
    }
}
