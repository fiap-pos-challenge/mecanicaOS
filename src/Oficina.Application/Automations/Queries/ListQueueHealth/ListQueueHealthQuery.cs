using MediatR;
using Oficina.Application.Automations.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Automations.Queries.ListQueueHealth;

public sealed record ListQueueHealthQuery : IRequest<Result<QueueHealthListDto>>;
