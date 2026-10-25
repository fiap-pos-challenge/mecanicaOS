namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderPhotoContentDto(Guid Id, string ContentType, string DataUrl, string Caption);
