namespace Oficina.Application.Finance.Dtos;

public sealed record FinanceSummaryDto(
    string Period,
    string From,
    string To,
    FinanceKpisDto Kpis,
    IReadOnlyList<PaymentMethodTotalDto> ByMethod,
    IReadOnlyList<ReceiptDto> Receipts,
    IReadOnlyList<ReceivableDto> Receivables,
    IReadOnlyList<CommissionDto> Commissions);
