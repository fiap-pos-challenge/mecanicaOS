namespace Oficina.Application.Finance.Dtos;

public sealed record PlatformUsageDto(int Customers, int Vehicles, int ServiceOrdersThisMonth, int DocumentsThisMonth, int ActiveAutomations);
