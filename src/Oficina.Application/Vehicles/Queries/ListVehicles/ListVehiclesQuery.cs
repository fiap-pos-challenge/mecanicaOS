using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dto;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Queries.ListVehicles;

public sealed record ListVehiclesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<VehicleDto>>>;
