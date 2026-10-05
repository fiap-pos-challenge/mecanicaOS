using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommandHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (await customers.GetByIdAsync(command.Id, cancellationToken) is not { } customer)
        {
            return CustomerErrors.NotFound;
        }

        var email = CustomerMapper.ToOptionalEmail(command.Email);
        var phone = Phone.Create(command.Phone);
        var address = CustomerMapper.ToAddress(command.Address);
        if ((email.Error ?? phone.Error ?? address.Error) is { } invalidData)
        {
            return invalidData;
        }

        if (customer.ChangeContact(command.Name, email.Value, phone.Value).Error is { } invalidContact)
        {
            return invalidContact;
        }

        customer.ChangeAddress(address.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return customer.ToDto();
    }
}
