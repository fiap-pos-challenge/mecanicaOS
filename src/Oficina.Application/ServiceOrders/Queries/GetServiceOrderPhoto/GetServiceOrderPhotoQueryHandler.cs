using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.ServiceOrders.Queries.GetServiceOrderPhoto;

public sealed class GetServiceOrderPhotoQueryHandler(IServiceOrderPhotoRepository photos) : IRequestHandler<GetServiceOrderPhotoQuery, Result<ServiceOrderPhotoContentDto>>
{
    public async Task<Result<ServiceOrderPhotoContentDto>> Handle(GetServiceOrderPhotoQuery query, CancellationToken cancellationToken) =>
        await photos.GetAsync(query.ServiceOrderId, query.PhotoId, cancellationToken) is { } photo
            ? new ServiceOrderPhotoContentDto(photo.Id, photo.ContentType, photo.DataUrl, photo.Caption)
            : ServiceOrderErrors.PhotoNotFound;
}
