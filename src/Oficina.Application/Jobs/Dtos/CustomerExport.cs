namespace Oficina.Application.Jobs.Dtos;

public sealed record CustomerExport(Guid CustomerId, DateTimeOffset GeneratedAt, ExportCustomer Customer, IReadOnlyList<ExportVehicle> Vehicles, IReadOnlyList<ExportServiceOrder> ServiceOrders, IReadOnlyList<ExportAccess> AccessLog);
