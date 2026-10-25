using NSubstitute;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Catalog.Queries.GetPackageById;
using Oficina.Application.Catalog.Queries.GetPackagePriceHistory;
using Oficina.Application.Catalog.Queries.GetPartById;
using Oficina.Application.Catalog.Queries.GetPartPriceHistory;
using Oficina.Application.Catalog.Queries.ListPackages;
using Oficina.Application.Catalog.Queries.ListParts;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Inventory.Errors;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Queries;

public sealed class PartAndPackageQueryHandlerTests
{
    private readonly IPartQueries _parts = Substitute.For<IPartQueries>();
    private readonly IPackageQueries _packages = Substitute.For<IPackageQueries>();
    private readonly IPriceHistoryQueries _history = Substitute.For<IPriceHistoryQueries>();
    private readonly PartDto _part = ServiceOrderBuilder.CreatePart().ToDto(includeCost: false);
    private readonly PackageDto _package = new(Guid.CreateVersion7(), "Revisão", null, [], 100m, 100m, 0m, null, true, DateTimeOffset.UnixEpoch, null);
    private readonly IReadOnlyList<PriceChangeDto> _changes = [new("UnitPrice", null, 10m, "Ana", DateTimeOffset.UnixEpoch)];

    [Fact]
    public void PartMapper_HidesTheCostWhenAsked()
    {
        var part = ServiceOrderBuilder.CreatePart();

        Assert.Null(part.ToDto(includeCost: false).Cost);
        Assert.Equal(part.Cost.Amount, part.ToDto(includeCost: true).Cost);
    }

    [Fact]
    public async Task Parts_ReturnTheQueriesResults()
    {
        var page = new PagedResult<PartDto>([_part], 1, 10, 1);
        _parts.ListAsync(new PageRequest(1, 10), new CatalogFilter("filtro", false), true, Arg.Any<CancellationToken>()).Returns(page);
        _parts.GetByIdAsync(_part.Id, true, Arg.Any<CancellationToken>()).Returns(_part);
        _history.ListAsync(CatalogItemKind.Part, _part.Id, true, Arg.Any<CancellationToken>()).Returns(_changes);

        Assert.Same(page, (await new ListPartsQueryHandler(_parts).Handle(new ListPartsQuery(1, 10, false, " filtro ", CatalogStatus.Inactive, true), CancellationToken.None)).Value);
        Assert.Same(_part, (await new GetPartByIdQueryHandler(_parts).Handle(new GetPartByIdQuery(_part.Id, true), CancellationToken.None)).Value);
        Assert.Equal(_changes, (await new GetPartPriceHistoryQueryHandler(_parts, _history).Handle(new GetPartPriceHistoryQuery(_part.Id, true), CancellationToken.None)).Value.Items);
        Assert.Equal(InventoryErrors.NotFound, (await new GetPartByIdQueryHandler(_parts).Handle(new GetPartByIdQuery(Guid.CreateVersion7(), true), CancellationToken.None)).Error);
        Assert.Equal(InventoryErrors.NotFound, (await new GetPartPriceHistoryQueryHandler(_parts, _history).Handle(new GetPartPriceHistoryQuery(Guid.CreateVersion7(), true), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Packages_ReturnTheQueriesResults()
    {
        var page = new PagedResult<PackageDto>([_package], 1, 10, 1);
        _packages.ListAsync(new PageRequest(1, 10), new CatalogFilter(null, null), Arg.Any<CancellationToken>()).Returns(page);
        _packages.GetByIdAsync(_package.Id, Arg.Any<CancellationToken>()).Returns(_package);
        _history.ListAsync(CatalogItemKind.Package, _package.Id, false, Arg.Any<CancellationToken>()).Returns(_changes);

        Assert.Same(page, (await new ListPackagesQueryHandler(_packages).Handle(new ListPackagesQuery(1, 10, true, null, null), CancellationToken.None)).Value);
        Assert.Same(_package, (await new GetPackageByIdQueryHandler(_packages).Handle(new GetPackageByIdQuery(_package.Id), CancellationToken.None)).Value);
        Assert.Equal(_changes, (await new GetPackagePriceHistoryQueryHandler(_packages, _history).Handle(new GetPackagePriceHistoryQuery(_package.Id), CancellationToken.None)).Value.Items);
        Assert.Equal(PackageErrors.NotFound, (await new GetPackageByIdQueryHandler(_packages).Handle(new GetPackageByIdQuery(Guid.CreateVersion7()), CancellationToken.None)).Error);
        Assert.Equal(PackageErrors.NotFound, (await new GetPackagePriceHistoryQueryHandler(_packages, _history).Handle(new GetPackagePriceHistoryQuery(Guid.CreateVersion7()), CancellationToken.None)).Error);
    }

    [Fact]
    public void Validators_RejectInvalidPagesTermsAndIds()
    {
        var term = new string('t', 121);

        Assert.Equal(3, new ListPartsQueryValidator().Validate(new ListPartsQuery(0, 0, false, term, null, false)).Errors.Count);
        Assert.Equal(3, new ListPackagesQueryValidator().Validate(new ListPackagesQuery(0, 0, false, term, null)).Errors.Count);
        Assert.Single(new GetPartByIdQueryValidator().Validate(new GetPartByIdQuery(Guid.Empty, false)).Errors);
        Assert.Single(new GetPartPriceHistoryQueryValidator().Validate(new GetPartPriceHistoryQuery(Guid.Empty, false)).Errors);
        Assert.Single(new GetPackageByIdQueryValidator().Validate(new GetPackageByIdQuery(Guid.Empty)).Errors);
        Assert.Single(new GetPackagePriceHistoryQueryValidator().Validate(new GetPackagePriceHistoryQuery(Guid.Empty)).Errors);
    }
}
