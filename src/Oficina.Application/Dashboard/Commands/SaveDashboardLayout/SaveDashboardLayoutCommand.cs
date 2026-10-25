using MediatR;
using Oficina.Application.Dashboard.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Dashboard.Commands.SaveDashboardLayout;

public sealed record SaveDashboardLayoutCommand(IReadOnlyList<DashboardCardDto>? Cards) : IRequest<Result<DashboardLayoutDto>>;
