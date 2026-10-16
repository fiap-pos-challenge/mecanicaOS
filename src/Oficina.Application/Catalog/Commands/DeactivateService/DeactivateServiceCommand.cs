using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.DeactivateService;

public sealed record DeactivateServiceCommand(Guid Id) : IRequest<Result>;
