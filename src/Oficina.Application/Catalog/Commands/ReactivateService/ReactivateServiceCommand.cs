using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.ReactivateService;

public sealed record ReactivateServiceCommand(Guid Id) : IRequest<Result>;
