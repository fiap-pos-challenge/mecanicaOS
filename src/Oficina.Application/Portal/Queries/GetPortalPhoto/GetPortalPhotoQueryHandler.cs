using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Portal.Queries.GetPortalPhoto;

public sealed class GetPortalPhotoQueryHandler(PortalAccess access, IServiceOrderPhotoRepository photos) : IRequestHandler<GetPortalPhotoQuery, Result<ServiceOrderPhotoContentDto>>
{
    public async Task<Result<ServiceOrderPhotoContentDto>> Handle(GetPortalPhotoQuery query, CancellationToken cancellationToken)
    {
        var estimate = await access.SessionAsync(query.Session, cancellationToken);
        if (estimate.Error is { } sessionError)
        {
            return sessionError;
        }

        return await photos.GetAsync(estimate.Value.ServiceOrderId, query.PhotoId, cancellationToken) is { } photo
            ? new ServiceOrderPhotoContentDto(photo.Id, photo.ContentType, photo.DataUrl, photo.Caption)
            : ServiceOrderErrors.PhotoNotFound;
    }
}
