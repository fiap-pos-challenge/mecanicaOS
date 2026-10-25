namespace Oficina.Application.Automations;

public static class AutomationEventTexts
{
    public const string OpenOrder = "Abra a OS para ver os detalhes.";
    public const string QuoteApprovedBody = "O cliente aprovou o orçamento. A OS pode seguir para execução.";
    public const string QuoteRejectedBody = "O cliente recusou o orçamento. Revise o diagnóstico ou crie uma nova versão.";
    public const string StockLowBody = "O estoque ficou abaixo do mínimo. Veja a sugestão de reposição.";

    public static string Ready(long number) => $"OS nº {number} pronta para entrega";

    public static string WaitingApproval(long number) => $"OS nº {number} aguarda aprovação do cliente";

    public static string QuoteApproved(long number) => $"Orçamento da OS nº {number} aprovado";

    public static string QuoteRejected(long number) => $"Orçamento da OS nº {number} recusado";

    public static string StockLow(string partName) => $"{partName} abaixo do estoque mínimo";
}
