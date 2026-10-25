namespace Oficina.Application.Quotes.Dtos;

public sealed record OpenServiceOrderDto(Guid Id, long Number, string CustomerName, string LicensePlate, string Vehicle);
