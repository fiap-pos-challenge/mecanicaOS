using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(Guid Id, string Name, string? Document, string? Email, string? Phone, int LeadTimeDays) : IRequest<Result<SupplierDto>>;
