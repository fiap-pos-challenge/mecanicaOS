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

namespace Oficina.Application.Catalog.Commands.CreatePackage;

public sealed class CreatePackageCommandHandler(
    IPackageRepository packages,
    IServiceRepository services,
    IPartRepository parts,
    IPackageQueries queries,
    IPriceChangeRepository priceChanges,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreatePackageCommand, Result<PackageDto>>
{
    public async Task<Result<PackageDto>> Handle(CreatePackageCommand command, CancellationToken cancellationToken)
    {
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

        var package = Package.Create(command.Name, command.Description, price.Value, items.Value, command.MinMileage, command.MaxMileage);
        if (package.Error is { } invalidPackage)
        {
            return invalidPackage;
        }

        packages.Add(package.Value);
        priceChanges.Add(PriceChange.Record(CatalogItemKind.Package, package.Value.Id, PriceField.PackagePrice, null, package.Value.Price, currentUser.UserId, timeProvider.GetUtcNow()));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await queries.GetByIdAsync(package.Value.Id, cancellationToken) is { } created ? created : PackageErrors.NotFound;
    }
}
