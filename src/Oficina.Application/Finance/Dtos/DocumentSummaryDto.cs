namespace Oficina.Application.Finance.Dtos;

public sealed record DocumentSummaryDto(string Code, string Kind, long Number, string CustomerName, decimal Total, DateTimeOffset IssuedAt);
