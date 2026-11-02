using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Application.Catalog.Commands.CreateService;

public sealed class CreateServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateServiceCommand, Result<ServiceDto>>
{
    public async Task<Result<ServiceDto>> Handle(CreateServiceCommand command, CancellationToken cancellationToken)
    {
        var basePrice = Money.Create(command.BasePrice);
        if (basePrice.Error is { } invalidPrice)
        {
            return invalidPrice;
        }

        var service = Service.Create(command.Name, command.Description, basePrice.Value, command.EstimatedMinutes);
        if (service.Error is { } invalidService)
        {
            return invalidService;
        }

        services.Add(service.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return service.Value.ToDto();
    }
}
