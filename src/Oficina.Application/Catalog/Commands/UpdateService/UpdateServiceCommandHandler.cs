using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Application.Catalog.Commands.UpdateService;

public sealed class UpdateServiceCommandHandler(IServiceRepository services, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateServiceCommand, Result<ServiceDto>>
{
    public async Task<Result<ServiceDto>> Handle(UpdateServiceCommand command, CancellationToken cancellationToken)
    {
        if (await services.GetByIdAsync(command.Id, cancellationToken) is not { } service)
        {
            return CatalogErrors.NotFound;
        }

        var basePrice = Money.Create(command.BasePrice);
        if (basePrice.Error is { } invalidPrice)
        {
            return invalidPrice;
        }

        if ((service.ChangeDetails(command.Name, command.Description, command.EstimatedMinutes).Error
            ?? service.ChangePrice(basePrice.Value).Error) is { } invalidService)
        {
            return invalidService;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return service.ToDto();
    }
}
