using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record OpenServiceOrderRequest(Guid CustomerId, Guid VehicleId, string ProblemDescription, DateTimeOffset? PromisedAt, int? Mileage);

public sealed record TransitionRequest(ServiceOrderStatus To, int Version, string? Reason);

public sealed record ChecklistRequest(IReadOnlyList<ChecklistChange> Items);

public sealed record PhotoRequest(string? ClientId, string FileName, string ContentType, string DataUrl);

public sealed record DiagnosisRequest(string Description);

public sealed record SendToQuoteRequest(string? Description, IReadOnlyList<QuoteRequestItem>? Items);
