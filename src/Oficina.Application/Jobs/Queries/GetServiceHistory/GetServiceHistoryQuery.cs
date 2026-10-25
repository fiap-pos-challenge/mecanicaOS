using MediatR;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Jobs.Queries.GetServiceHistory;

public sealed record GetServiceHistoryQuery(Guid CustomerId) : IRequest<Result<ServiceHistoryDto>>;
