using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands.UpdateService;

public sealed class UpdateServiceCommandHandler(
    IServiceRepository services,
    IPartRepository parts,
    IPriceChangeRepository priceChanges,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<UpdateServiceCommand, Result<ServiceDto>>
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

        var oldPrice = service.BasePrice;
        if ((service.ChangeDetails(command.Name, command.Description, command.EstimatedMinutes, command.CommissionPercent, command.WarrantyDays).Error
            ?? service.ChangePrice(basePrice.Value).Error) is { } invalidService)
        {
            return invalidService;
        }

        var usualParts = await UsualPartsResolver.ResolveAsync(parts, command.UsualParts, cancellationToken);
        if ((usualParts.Error ?? service.ReplaceUsualParts(usualParts.Value).Error) is { } invalidParts)
        {
            return invalidParts;
        }

        priceChanges.Add(PriceChange.Record(CatalogItemKind.Service, service.Id, PriceField.BasePrice, oldPrice, service.BasePrice, currentUser.UserId, timeProvider.GetUtcNow()));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return service.ToDto();
    }
}
