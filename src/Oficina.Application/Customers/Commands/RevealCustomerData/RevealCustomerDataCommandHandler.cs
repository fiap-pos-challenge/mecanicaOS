using System.Diagnostics;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Privacy.Entities;
using Oficina.Domain.Privacy.Enums;
using Oficina.Domain.Privacy.Repositories;

namespace Oficina.Application.Customers.Commands.RevealCustomerData;

public sealed class RevealCustomerDataCommandHandler(
    ICustomerRepository customers,
    ISensitiveDataAccessRepository accesses,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RevealCustomerDataCommand, Result<RevealedDataDto>>
{
    public async Task<Result<RevealedDataDto>> Handle(RevealCustomerDataCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return CommonErrors.Unauthorized;
        }

        if (await customers.GetByIdAsync(command.CustomerId, cancellationToken) is not { } customer)
        {
            return CustomerErrors.NotFound;
        }

        var traceId = Activity.Current?.TraceId.ToString();
        accesses.Add(SensitiveDataAccess.Record(userId, customer.Id, command.Field, command.Reason, traceId, timeProvider.GetUtcNow()));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new RevealedDataDto(command.Field, ValueOf(customer, command.Field));
    }

    private static string? ValueOf(Customer customer, SensitiveField field) => field switch
    {
        SensitiveField.Document => customer.Document.Value,
        SensitiveField.Email => customer.Email?.Value,
        _ => customer.Phone.Value,
    };
}
