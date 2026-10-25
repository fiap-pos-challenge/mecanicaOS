using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Commands.AddPhoto;

public sealed class AddPhotoCommandHandler(
    IServiceOrderRepository serviceOrders,
    IServiceOrderPhotoRepository photos,
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

        var clientId = string.IsNullOrWhiteSpace(command.ClientId) ? null : command.ClientId.Trim();
        if (clientId is null || !await photos.ExistsAsync(order.Id, clientId, cancellationToken))
        {
            var photo = ServiceOrderPhoto.Take(order, clientId, command.ContentType, command.DataUrl, command.FileName, timeProvider.GetUtcNow());
            if (photo.Error is { } invalid)
            {
                return invalid;
            }

            photos.Add(photo.Value);
            if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
            {
                return commitError;
            }
        }

        return await ServiceOrderResponses.WorkAsync(queries, order.Id, cancellationToken);
    }
}
