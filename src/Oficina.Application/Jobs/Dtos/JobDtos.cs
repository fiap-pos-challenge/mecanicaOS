namespace Oficina.Application.Jobs.Dtos;

public sealed record ImportErrorDto(int Line, string Field, string Message);

public sealed record JobResultDto(string? FileName, string? Content, int? TotalLines, int? ValidLines, IReadOnlyList<ImportErrorDto>? Errors);

public sealed record JobDto(Guid Id, string Kind, string Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, JobResultDto? Result);

public sealed record ImportReport(int TotalLines, int ValidLines, IReadOnlyList<ImportErrorDto> Errors);

public sealed record CustomerSummaryDto(Guid CustomerId, int VehicleCount, DateTimeOffset? LastVisitAt, decimal TotalSpent, decimal AverageTicket, int ServiceOrderCount, int OpenServiceOrders, string Situation, bool Fleet);

public sealed record CustomerSummaryListDto(IReadOnlyList<CustomerSummaryDto> Items);

public sealed record ServiceHistoryEntryDto(long Number, string Description, decimal Total, string Status, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, string? PaymentMethod);

public sealed record ServiceHistoryDto(IReadOnlyList<ServiceHistoryEntryDto> Items);

public sealed record ExportCustomer(string Name, string Document, string DocumentType, string? Email, string Phone, object? Address, bool ConsentWhatsApp, bool ConsentEmail, DateTimeOffset? ConsentUpdatedAt, DateTimeOffset CreatedAt);

public sealed record ExportVehicle(string LicensePlate, string Brand, string Model, int Year);

public sealed record ExportServiceOrder(long Number, string Status, DateTimeOffset OpenedAt, decimal Total);

public sealed record ExportAccess(DateTimeOffset OccurredAt, string UserName, string Field, string Reason);

public sealed record CustomerExport(Guid CustomerId, DateTimeOffset GeneratedAt, ExportCustomer Customer, IReadOnlyList<ExportVehicle> Vehicles, IReadOnlyList<ExportServiceOrder> ServiceOrders, IReadOnlyList<ExportAccess> AccessLog);
