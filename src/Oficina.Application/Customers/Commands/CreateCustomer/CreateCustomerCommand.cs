using MediatR;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    string Name,
    string Document,
    string? Email,
    string Phone,
    AddressInput? Address) : IRequest<Result<CustomerDto>>;
