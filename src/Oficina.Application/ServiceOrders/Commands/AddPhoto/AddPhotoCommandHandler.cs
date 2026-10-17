using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.AddPhoto;

public sealed class AddPhotoCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceOrderBoardQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<AddPhotoCommand, Result<MechanicOrderDto>>
{
    public async Task<Result<MechanicOrderDto>> Handle(AddPhotoCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.Id, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        var result = order.AddPhoto(string.IsNullOrWhiteSpace(command.ClientId) ? null : command.ClientId.Trim(), command.ContentType, command.DataUrl, command.FileName, now);
        if (result.Error is { } invalid)
        {
            return invalid;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await ServiceOrderResponses.WorkAsync(queries, order.Id, cancellationToken);
    }
}
