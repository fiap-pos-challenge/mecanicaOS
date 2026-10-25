using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(string Name, string? Document, string? Email, string? Phone, int LeadTimeDays) : IRequest<Result<SupplierDto>>;
