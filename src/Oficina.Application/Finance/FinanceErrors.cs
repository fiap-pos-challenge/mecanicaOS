using Oficina.Domain.Common.Results;

namespace Oficina.Application.Finance;

public static class FinanceErrors
{
    public static readonly Error InvalidPeriod = new("finance.period", "Período inválido.", ErrorType.Validation,
        [new FieldError("period", "finance.period", "Escolha hoje, semana, mês ou trimestre.")]);

    public static readonly Error InvalidKind = new("documents.kind", "Tipo de documento inválido.", ErrorType.Validation,
        [new FieldError("kind", "documents.kind", "Escolha OS, orçamento ou recibo.")]);
}
