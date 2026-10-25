using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Reports;

public static class ReportErrors
{
    public static readonly Error NotFound = new("reports.notFound", "Relatório não encontrado.", ErrorType.NotFound);

    public static readonly Error InvalidDefinition = new("reports.definition", "Relatório inválido.", ErrorType.Validation,
        [new FieldError("definition", "reports.definition", "Relatório inválido.")]);

    public static readonly Error InvalidName = new("reports.name", "Dê um nome com 3 a 80 caracteres.", ErrorType.Validation,
        [new FieldError("name", "reports.name", "Dê um nome com 3 a 80 caracteres.")]);

    public static readonly Error DuplicateName = new("reports.duplicateName", "Já existe um relatório com este nome.", ErrorType.Conflict);

    public static readonly Error InvalidFormat = new("reports.format", "Escolha Excel (xlsx) ou PDF.", ErrorType.Validation,
        [new FieldError("format", "reports.format", "Escolha Excel (xlsx) ou PDF.")]);

    public static readonly Error NotUnderstood = new("assistant.notUnderstood", "Não entendi a pergunta. Tente citar o que medir, como faturamento, ticket médio ou quantidade de OS.", ErrorType.BusinessRule);

    public static readonly Error InvalidQuestion = new("assistant.question", "Escreva a pergunta com 8 a 300 caracteres.", ErrorType.Validation,
        [new FieldError("question", "assistant.question", "Escreva a pergunta com 8 a 300 caracteres.")]);

    public static Error Incompatible(string message) => new("reports.incompatible", message, ErrorType.BusinessRule);
}
