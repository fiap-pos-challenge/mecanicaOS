using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.Services;

namespace Oficina.Application.Customers.Commands.DeleteCustomer;

public sealed class DeleteCustomerCommandHandler(
    ICustomerRepository customers,
    RegistrationDeletionPolicy deletionPolicy,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteCustomerCommand, Result>
{
    public async Task<Result> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken)
    {
        if (await customers.GetByIdAsync(command.Id, cancellationToken) is not { } customer)
        {
            return CustomerErrors.NotFound;
        }

        var canDelete = await deletionPolicy.EnsureCanDeleteCustomerAsync(customer, cancellationToken);
        if (canDelete.IsFailure)
        {
            return canDelete;
        }

        customers.Remove(customer);
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
