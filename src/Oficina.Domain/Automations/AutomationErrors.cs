using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Automations;

public static class AutomationErrors
{
    public static readonly Error NotFound = new("automations.notFound", "Automação não encontrada.", ErrorType.NotFound);

    public static readonly Error InvalidDefinition = new("automations.definition", "Regra inválida.", ErrorType.Validation,
        [new FieldError("definition", "automations.definition", "Regra inválida.")]);

    public static readonly Error InvalidEnabled = new("automations.enabled", "Informe se a regra fica ligada.", ErrorType.Validation,
        [new FieldError("enabled", "automations.enabled", "Informe se a regra fica ligada.")]);

    public static readonly Error EmailFailed = new("automations.emailFailed", "Não foi possível enviar o e-mail.", ErrorType.Unexpected);

    public static readonly Error NoPart = new("automations.noPart", "A regra não encontrou a peça.", ErrorType.BusinessRule);

    public static Error Invalid(string message) => new("automations.invalid", message, ErrorType.BusinessRule);
}
