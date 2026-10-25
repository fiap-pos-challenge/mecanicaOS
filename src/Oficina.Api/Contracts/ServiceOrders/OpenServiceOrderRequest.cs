namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record OpenServiceOrderRequest(Guid CustomerId, Guid VehicleId, string ProblemDescription, DateTimeOffset? PromisedAt, int? Mileage);
