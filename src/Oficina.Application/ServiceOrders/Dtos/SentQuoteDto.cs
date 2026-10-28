namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record SentQuoteDto(string ApprovalToken, ServiceOrderDetailDto ServiceOrder);
