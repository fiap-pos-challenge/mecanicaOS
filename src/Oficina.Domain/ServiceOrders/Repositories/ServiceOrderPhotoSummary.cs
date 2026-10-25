namespace Oficina.Domain.ServiceOrders.Repositories;

public sealed record ServiceOrderPhotoSummary(Guid Id, Guid ServiceOrderId, string Caption, DateTimeOffset TakenAt);
