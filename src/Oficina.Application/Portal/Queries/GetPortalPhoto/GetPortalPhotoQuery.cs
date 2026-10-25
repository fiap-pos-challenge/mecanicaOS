using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Queries.GetPortalPhoto;

public sealed record GetPortalPhotoQuery(string? Session, Guid PhotoId) : IRequest<Result<ServiceOrderPhotoContentDto>>;
