using MediatR;
using Oficina.Application.Dashboard.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Dashboard.Queries.GetDashboardLayout;

public sealed record GetDashboardLayoutQuery : IRequest<Result<DashboardLayoutDto>>;
