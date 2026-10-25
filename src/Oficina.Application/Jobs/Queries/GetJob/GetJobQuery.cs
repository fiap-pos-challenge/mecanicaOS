using MediatR;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Jobs.Queries.GetJob;

public sealed record GetJobQuery(Guid Id) : IRequest<Result<JobDto>>;
