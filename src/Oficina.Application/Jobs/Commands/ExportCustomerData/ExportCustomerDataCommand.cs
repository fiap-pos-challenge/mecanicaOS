using MediatR;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Jobs.Commands.ExportCustomerData;

public sealed record ExportCustomerDataCommand(Guid CustomerId) : IRequest<Result<JobDto>>;
