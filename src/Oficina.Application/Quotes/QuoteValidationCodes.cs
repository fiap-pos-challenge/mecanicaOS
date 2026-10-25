namespace Oficina.Application.Quotes;

public static class QuoteValidationCodes
{
    public const string Status = "quotes.invalidStatus";
    public const string StatusMessage = "Filtre por Pending, Approved ou Rejected.";
    public const string Id = "quotes.idRequired";
    public const string IdMessage = "Informe o orçamento.";
    public const string ServiceOrder = "quotes.serviceOrderRequired";
    public const string ServiceOrderMessage = "Informe a OS.";
    public const string Items = "quotes.itemsRequired";
    public const string ItemsMessage = "Informe a lista de itens.";
}
