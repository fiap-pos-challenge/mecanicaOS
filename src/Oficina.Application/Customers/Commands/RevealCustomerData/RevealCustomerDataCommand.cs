using MediatR;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Privacy.Enums;

namespace Oficina.Application.Customers.Commands.RevealCustomerData;

public sealed record RevealCustomerDataCommand(Guid CustomerId, SensitiveField Field, AccessReason Reason) : IRequest<Result<RevealedDataDto>>;
