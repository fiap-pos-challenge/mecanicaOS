namespace Oficina.Application.Jobs.Dtos;

public sealed record CustomerSummaryDto(Guid CustomerId, int VehicleCount, DateTimeOffset? LastVisitAt, decimal TotalSpent, decimal AverageTicket, int ServiceOrderCount, int OpenServiceOrders, string Situation, bool Fleet);
