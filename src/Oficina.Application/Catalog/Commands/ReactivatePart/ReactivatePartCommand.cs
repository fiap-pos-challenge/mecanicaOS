using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.ReactivatePart;

public sealed record ReactivatePartCommand(Guid Id) : IRequest<Result>;
