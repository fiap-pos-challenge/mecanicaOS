namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderPhotoDto(Guid Id, string Url, string Caption, DateTimeOffset TakenAt);
