using MediatR;
using Oficina.Application.Dashboard.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Dashboard.Queries.GetDashboardSnapshot;

public sealed record GetDashboardSnapshotQuery(string? Period) : IRequest<Result<DashboardSnapshotDto>>;
