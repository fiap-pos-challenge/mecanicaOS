using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Commands.RetryQueueFailure;

public sealed record RetryQueueFailureCommand(Guid Id) : IRequest<Result>;
