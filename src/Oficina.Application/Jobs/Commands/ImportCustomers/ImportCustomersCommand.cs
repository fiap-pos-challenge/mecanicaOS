using MediatR;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Jobs.Commands.ImportCustomers;

public sealed record ImportCustomersCommand(string? FileName, string? Csv) : IRequest<Result<JobDto>>;
