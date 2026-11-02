using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Customers.Commands.DeleteCustomer;

public sealed record DeleteCustomerCommand(Guid Id) : IRequest<Result>;
