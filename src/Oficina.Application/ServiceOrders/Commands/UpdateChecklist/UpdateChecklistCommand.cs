using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.UpdateChecklist;

public sealed record UpdateChecklistCommand(Guid Id, IReadOnlyList<ChecklistChange> Items) : IRequest<Result<MechanicOrderDto>>;
