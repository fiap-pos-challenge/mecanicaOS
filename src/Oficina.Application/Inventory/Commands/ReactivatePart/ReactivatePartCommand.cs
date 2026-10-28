using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.ReactivatePart;

public sealed record ReactivatePartCommand(Guid Id) : IRequest<Result>;
