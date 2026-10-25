using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
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
        var now = timeProvider.GetUtcNow();
        if (command.PromisedAt is { } promised && promised <= now)
        {
            return ServiceOrderErrors.InvalidPromise;
        }

        if (await customers.GetByIdAsync(command.CustomerId, cancellationToken) is not { } customer)
        {
            return CustomerErrors.NotFound;
        }

        if (await vehicles.GetByIdAsync(command.VehicleId, cancellationToken) is not { } vehicle)
        {
            return VehicleErrors.NotFound;
        }

        if ((await policy.EnsureCanOpenAsync(customer, vehicle, cancellationToken)).Error is { } policyError)
        {
            return policyError;
        }

        var number = await serviceOrders.NextNumberAsync(cancellationToken);
        var order = ServiceOrder.Open(number, customer.Id, vehicle.Id, command.ProblemDescription, currentUser.UserId, now);
        if (order.Error is { } invalid)
        {
            return invalid;
        }

        order.Value.Plan(command.PromisedAt, command.Mileage ?? vehicle.Mileage);
        serviceOrders.Add(order.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await ServiceOrderResponses.DetailAsync(queries, order.Value.Id, cancellationToken);
    }
}
