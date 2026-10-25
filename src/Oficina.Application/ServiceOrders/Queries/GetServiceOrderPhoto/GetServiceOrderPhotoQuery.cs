using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.GetServiceOrderPhoto;

public sealed record GetServiceOrderPhotoQuery(Guid ServiceOrderId, Guid PhotoId) : IRequest<Result<ServiceOrderPhotoContentDto>>;
