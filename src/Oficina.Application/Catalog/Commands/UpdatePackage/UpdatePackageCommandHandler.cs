using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands.UpdatePackage;

public sealed class UpdatePackageCommandHandler(
    IPackageRepository packages,
    IServiceRepository services,
    IPartRepository parts,
    IPackageQueries queries,
    IPriceChangeRepository priceChanges,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<UpdatePackageCommand, Result<PackageDto>>
{
    public async Task<Result<PackageDto>> Handle(UpdatePackageCommand command, CancellationToken cancellationToken)
    {
        if (await packages.GetByIdAsync(command.Id, cancellationToken) is not { } package)
        {
            return PackageErrors.NotFound;
        }

        var price = Money.Create(command.Price);
        if (price.Error is { } invalidPrice)
        {
            return invalidPrice;
        }

        var items = await new PackageItemsResolver(services, parts).ResolveAsync(command.Items, cancellationToken);
        if (items.Error is { } invalidItems)
        {
            return invalidItems;
        }

        var oldPrice = package.Price;
        if (package.Change(command.Name, command.Description, price.Value, items.Value, command.MinMileage, command.MaxMileage).Error is { } invalidPackage)
        {
            return invalidPackage;
        }

        priceChanges.Add(PriceChange.Record(CatalogItemKind.Package, package.Id, PriceField.PackagePrice, oldPrice, package.Price, currentUser.UserId, timeProvider.GetUtcNow()));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await queries.GetByIdAsync(package.Id, cancellationToken) is { } updated ? updated : PackageErrors.NotFound;
    }
}
