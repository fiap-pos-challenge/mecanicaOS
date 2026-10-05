using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var document = Document.Create(command.Document);
        var email = CustomerMapper.ToOptionalEmail(command.Email);
        var phone = Phone.Create(command.Phone);
        var address = CustomerMapper.ToAddress(command.Address);
        if ((document.Error ?? email.Error ?? phone.Error ?? address.Error) is { } invalidData)
        {
            return invalidData;
        }

        if (await customers.GetByDocumentAsync(document.Value, cancellationToken) is not null)
        {
            return CustomerErrors.DuplicateDocument;
        }

        var customer = Customer.Create(command.Name, document.Value, email.Value, phone.Value, address.Value);
        if (customer.Error is { } invalidCustomer)
        {
            return invalidCustomer;
        }

        customers.Add(customer.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return customer.Value.ToDto();
    }
}
