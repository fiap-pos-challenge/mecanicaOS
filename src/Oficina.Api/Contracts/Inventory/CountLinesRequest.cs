using Oficina.Application.Inventory.Dtos;

namespace Oficina.Api.Contracts.Inventory;

public sealed record CountLinesRequest(IReadOnlyList<CountLineInput>? Lines);
