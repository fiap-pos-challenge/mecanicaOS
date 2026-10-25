namespace Oficina.Application.Jobs;

public static class JobMessages
{
    public const string Header = "O cabeçalho precisa ser nome;documento;telefone;email.";
    public const string Name = "Informe o nome.";
    public const string Document = "CPF ou CNPJ inválido.";
    public const string Phone = "Telefone inválido.";
    public const string Email = "E-mail inválido.";
    public const string DuplicateDocument = "Já existe cliente com este documento.";
    public const string Invalid = "Dados inválidos.";

    public static string ExportFileName(Guid customerId) => $"dados-titular-{customerId.ToString("N")[..8]}.json";
}
