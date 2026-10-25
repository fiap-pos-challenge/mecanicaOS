namespace Oficina.Application.Jobs.Dtos;

public sealed record CustomerSummaryListDto(IReadOnlyList<CustomerSummaryDto> Items);
