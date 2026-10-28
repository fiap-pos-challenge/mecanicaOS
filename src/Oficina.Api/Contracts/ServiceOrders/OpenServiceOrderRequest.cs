namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record OpenServiceOrderRequest(string CustomerDocument, string LicensePlate, string ProblemDescription);
