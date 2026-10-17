using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands.CreateService;

public sealed class CreateServiceCommandHandler(
    IServiceRepository services,
    IPartRepository parts,
    IPriceChangeRepository priceChanges,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreateServiceCommand, Result<ServiceDto>>
{
    public async Task<Result<ServiceDto>> Handle(CreateServiceCommand command, CancellationToken cancellationToken)
    {
        var basePrice = Money.Create(command.BasePrice);
        if (basePrice.Error is { } invalidPrice)
        {
            return invalidPrice;
        }

        var service = Service.Create(command.Name, command.Description, basePrice.Value, command.EstimatedMinutes, command.CommissionPercent, command.WarrantyDays);
        if (service.Error is { } invalidService)
        {
            return invalidService;
        }

        var usualParts = await UsualPartsResolver.ResolveAsync(parts, command.UsualParts, cancellationToken);
        if ((usualParts.Error ?? service.Value.ReplaceUsualParts(usualParts.Value).Error) is { } invalidParts)
        {
            return invalidParts;
        }

        services.Add(service.Value);
        priceChanges.Add(PriceChange.Record(CatalogItemKind.Service, service.Value.Id, PriceField.BasePrice, null, basePrice.Value, currentUser.UserId, timeProvider.GetUtcNow()));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return service.Value.ToDto();
    }
}
