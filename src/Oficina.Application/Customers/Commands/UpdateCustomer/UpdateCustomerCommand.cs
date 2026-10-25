using MediatR;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string? Email,
    string Phone,
    AddressInput? Address,
    MessagingConsentInput? MessagingConsent = null) : IRequest<Result<CustomerDto>>;
