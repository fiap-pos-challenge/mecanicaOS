using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.GetAverageExecutionTime;

public sealed record GetAverageExecutionTimeQuery(DateOnly? From, DateOnly? To) : IRequest<Result<AverageExecutionTimeDto>>;
