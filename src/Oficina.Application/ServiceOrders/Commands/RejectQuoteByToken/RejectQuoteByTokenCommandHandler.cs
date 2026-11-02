using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.ServiceOrders.Commands.RejectQuoteByToken;

public sealed class RejectQuoteByTokenCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceOrderQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RejectQuoteByTokenCommand, Result<PublicServiceOrderDto>>
{
    public async Task<Result<PublicServiceOrderDto>> Handle(RejectQuoteByTokenCommand command, CancellationToken cancellationToken)
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

        var rejected = serviceOrder.RejectQuote(command.Reason, null, timeProvider.GetUtcNow());
        if (rejected.Error is { } rejectionError)
        {
            return rejectionError;
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
