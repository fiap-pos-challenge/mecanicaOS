using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.Services;

namespace Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;

public sealed class OpenServiceOrderCommandHandler(
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    IServiceOrderRepository serviceOrders,
    ServiceOrderOpeningPolicy policy,
    IServiceOrderQueries queries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<OpenServiceOrderCommand, Result<ServiceOrderDetailDto>>
{
    public async Task<Result<ServiceOrderDetailDto>> Handle(OpenServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var document = Document.Create(command.CustomerDocument);
        var plate = LicensePlate.Create(command.LicensePlate);
        if ((document.Error ?? plate.Error) is { } invalidData)
        {
            return invalidData;
        }

        if (await customers.GetByDocumentAsync(document.Value, cancellationToken) is not { } customer)
        {
            return CustomerErrors.NotFound;
        }

        if (await vehicles.GetByLicensePlate(plate.Value, cancellationToken) is not { } vehicle)
        {
            return VehicleErrors.NotFound;
        }

        if ((await policy.EnsureCanOpenAsync(customer, vehicle, cancellationToken)).Error is { } policyError)
        {
            return policyError;
        }

        var number = await serviceOrders.NextNumberAsync(cancellationToken);
        var serviceOrder = ServiceOrder.Open(number, customer.Id, vehicle.Id, command.ProblemDescription, currentUser.UserId, timeProvider.GetUtcNow());
        if (serviceOrder.Error is { } invalidOrder)
        {
            return invalidOrder;
        }

        serviceOrders.Add(serviceOrder.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        if (await queries.GetByIdAsync(serviceOrder.Value.Id, cancellationToken) is not { } detail)
        {
            return ServiceOrderErrors.NotFound;
        }

        return detail;
    }
}
