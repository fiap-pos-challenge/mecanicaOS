using Oficina.Domain.Privacy.Enums;

namespace Oficina.Application.Customers.Dtos;

public sealed record RevealedDataDto(SensitiveField Field, string? Value);
