using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Dashboard;

public static class DashboardErrors
{
    public static readonly Error InvalidPeriod = new("dashboard.period", "Período inválido.", ErrorType.Validation,
        [new FieldError("period", "dashboard.period", "Escolha hoje, semana ou mês.")]);

    public static readonly Error InvalidLayout = new("dashboard.layout", "Layout inválido.", ErrorType.Validation,
        [new FieldError("cards", "dashboard.layout", "Layout inválido.")]);

    public static readonly Error AttentionNotFound = new("dashboard.attentionNotFound", "Item não encontrado.", ErrorType.NotFound);

    public static readonly Error ActionNotAllowed = new("dashboard.actionNotAllowed", "Esta ação não vale para este item.", ErrorType.BusinessRule);

    public static readonly Error NoSupplier = new("dashboard.noSupplier", "Cadastre um fornecedor ativo para criar o pedido de compra.", ErrorType.BusinessRule);
}
