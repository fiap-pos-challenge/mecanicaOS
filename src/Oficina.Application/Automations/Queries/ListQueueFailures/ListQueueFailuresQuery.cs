using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Queries.ListQueueFailures;

public sealed record ListQueueFailuresQuery : IRequest<Result<QueueFailureListDto>>;
