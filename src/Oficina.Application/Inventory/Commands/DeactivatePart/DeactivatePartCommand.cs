using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.DeactivatePart;

public sealed record DeactivatePartCommand(Guid Id) : IRequest<Result>;
