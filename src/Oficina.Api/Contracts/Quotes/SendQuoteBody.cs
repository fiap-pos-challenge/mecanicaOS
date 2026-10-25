namespace Oficina.Api.Contracts.Quotes;

public sealed record SendQuoteBody(string Channel, int ExpiresInDays, string Message);
