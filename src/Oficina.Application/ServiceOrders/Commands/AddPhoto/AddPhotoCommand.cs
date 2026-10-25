using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.AddPhoto;

public sealed record AddPhotoCommand(Guid Id, string? ClientId, string FileName, string ContentType, string DataUrl) : IRequest<Result<MechanicOrderDto>>;
