using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Jobs.Errors;

public static class JobErrors
{
    public static readonly Error NotFound = new("job.notFound", "Tarefa não encontrada.", ErrorType.NotFound);

    public static readonly Error InvalidFile = new("import.invalidFile", "Envie um arquivo CSV com cabeçalho nome;documento;telefone;email.", ErrorType.Validation,
        [new FieldError("file", "import.invalidFile", "Envie um arquivo CSV com cabeçalho nome;documento;telefone;email.")]);

    public static readonly Error FileTooLarge = new("import.fileTooLarge", "O arquivo passa de 2 MB.", ErrorType.TooLarge);

    public static readonly Error TooManyCustomers = new("insights.tooManyCustomers", "Envie até 500 clientes por consulta.", ErrorType.Validation,
        [new FieldError("customerIds", "insights.tooManyCustomers", "Envie até 500 clientes por consulta.")]);
}
