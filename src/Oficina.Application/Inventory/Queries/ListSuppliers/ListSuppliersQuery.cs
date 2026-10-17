using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListSuppliers;

public sealed record ListSuppliersQuery() : IRequest<Result<SupplierListDto>>;
