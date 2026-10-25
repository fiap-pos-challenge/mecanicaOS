namespace Oficina.Application.Dashboard.Dtos;

public sealed record AttentionItemDto(string Id, string Kind, string Severity, string Title, string Detail, string? Action, IReadOnlyDictionary<string, string>? Target, bool AiSuggested);
