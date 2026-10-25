namespace Oficina.Application.Finance.Dtos;

public sealed record FinanceKpisDto(long Received, long Receivable, int ReceivableCount, long Commissions, long AverageTicket, int Orders);
