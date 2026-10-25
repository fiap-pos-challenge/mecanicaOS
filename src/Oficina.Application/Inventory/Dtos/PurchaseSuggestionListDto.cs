namespace Oficina.Application.Inventory.Dtos;

public sealed record PurchaseSuggestionListDto(IReadOnlyList<PurchaseSuggestionDto> Items);
