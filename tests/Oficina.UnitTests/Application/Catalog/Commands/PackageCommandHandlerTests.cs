using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Commands.CreatePackage;
using Oficina.Application.Catalog.Commands.DeactivatePackage;
using Oficina.Application.Catalog.Commands.ReactivatePackage;
using Oficina.Application.Catalog.Commands.UpdatePackage;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class PackageCommandHandlerTests
{
    private readonly IPackageRepository _packages = Substitute.For<IPackageRepository>();
    private readonly IServiceRepository _services = Substitute.For<IServiceRepository>();
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly IPackageQueries _queries = Substitute.For<IPackageQueries>();
    private readonly IPriceChangeRepository _priceChanges = Substitute.For<IPriceChangeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Service _service = ServiceOrderBuilder.CreateService();
    private readonly Part _part = ServiceOrderBuilder.CreatePart();
    private readonly PackageDto _dto;

    public PackageCommandHandlerTests()
    {
        _dto = new PackageDto(Guid.CreateVersion7(), "Revisão", null, [], 100m, 125m, 25m, null, true, DateTimeOffset.UnixEpoch, null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _services.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Service> { _service });
        _parts.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Part> { _part });
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_dto);
    }

    private CreatePackageCommandHandler Create() => new(_packages, _services, _parts, _queries, _priceChanges, _currentUser, _unitOfWork, new FakeTimeProvider());

    private UpdatePackageCommandHandler Update() => new(_packages, _services, _parts, _queries, _priceChanges, _currentUser, _unitOfWork, new FakeTimeProvider());

    private CreatePackageCommand NewPackage(decimal partQuantity = 2m) => new(
        "Revisão",
        null,
        100m,
        [new PackageItemRequest(CatalogItemKind.Service, _service.Id, 1m), new PackageItemRequest(CatalogItemKind.Part, _part.Id, partQuantity)],
        null,
        null);

    private Package Existing()
    {
        var package = Package.Create("Revisão", null, Money.Create(100m).Value, [new(CatalogItemKind.Service, _service.Id, Quantity.Create(1m).Value)], null, null).Value;
        _packages.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        return package;
    }

    [Fact]
    public async Task Create_ValidCommand_AddsThePackageAndReturnsTheQuery()
    {
        var result = await Create().Handle(NewPackage(), CancellationToken.None);

        Assert.Same(_dto, result.Value);
        _packages.Received(1).Add(Arg.Is<Package>(package => package.Items.Count == 2));
        _priceChanges.Received(1).Add(Arg.Is<PriceChange>(change => change.Field == PriceField.PackagePrice && change.NewValue == 100m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_InvalidQuantity_ReturnsInvalidItems(decimal quantity) =>
        Assert.Equal(PackageErrors.InvalidItems, (await Create().Handle(NewPackage(quantity), CancellationToken.None)).Error);

    [Fact]
    public async Task Create_PackageKindOrInactiveItem_ReturnsInvalidItems()
    {
        _part.Deactivate();

        Assert.Equal(PackageErrors.InvalidItems, (await Create().Handle(NewPackage(), CancellationToken.None)).Error);
        Assert.Equal(PackageErrors.InvalidItems, (await Create().Handle(NewPackage() with { Items = [new PackageItemRequest(CatalogItemKind.Package, _service.Id, 1m)] }, CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Create_InvalidPriceOrData_ReturnsTheErrorWithoutCommit()
    {
        Assert.Equal(CommonErrors.NegativeAmount, (await Create().Handle(NewPackage() with { Price = -1m }, CancellationToken.None)).Error);
        Assert.Equal(PackageErrors.InvalidName, (await Create().Handle(NewPackage() with { Name = "" }, CancellationToken.None)).Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_CommitFailsOrQueryMisses_ReturnsTheError()
    {
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PackageDto?)null);
        Assert.Equal(PackageErrors.NotFound, (await Create().Handle(NewPackage(), CancellationToken.None)).Error);

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.Unexpected));
        Assert.Equal(CommonErrors.Unexpected, (await Create().Handle(NewPackage(), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Update_ValidCommand_ChangesAndRecordsTheNewPrice()
    {
        var package = Existing();
        var command = new UpdatePackageCommand(package.Id, "Revisão completa", "Tudo", 120m, NewPackage().Items, 0, 100);

        var result = await Update().Handle(command, CancellationToken.None);

        Assert.Same(_dto, result.Value);
        Assert.Equal("Revisão completa", package.Name);
        _priceChanges.Received(1).Add(Arg.Is<PriceChange>(change => change.OldValue == 100m && change.NewValue == 120m));
    }

    [Fact]
    public async Task Update_ErrorBranches_ReturnTheError()
    {
        var package = Existing();
        var command = new UpdatePackageCommand(package.Id, "Revisão", null, 100m, NewPackage().Items, null, null);

        Assert.Equal(PackageErrors.NotFound, (await Update().Handle(command with { Id = Guid.CreateVersion7() }, CancellationToken.None)).Error);
        Assert.Equal(CommonErrors.NegativeAmount, (await Update().Handle(command with { Price = -1m }, CancellationToken.None)).Error);
        Assert.Equal(PackageErrors.InvalidItems, (await Update().Handle(command with { Items = [new PackageItemRequest(CatalogItemKind.Part, _part.Id, 0m)] }, CancellationToken.None)).Error);
        Assert.Equal(PackageErrors.InvalidMileageRange, (await Update().Handle(command with { MinMileage = 10, MaxMileage = 1 }, CancellationToken.None)).Error);
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PackageDto?)null);
        Assert.Equal(PackageErrors.NotFound, (await Update().Handle(command, CancellationToken.None)).Error);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.Unexpected));
        Assert.Equal(CommonErrors.Unexpected, (await Update().Handle(command, CancellationToken.None)).Error);
    }

    [Fact]
    public async Task DeactivateAndReactivate_TogglesTheStatus()
    {
        var package = Existing();
        var deactivate = new DeactivatePackageCommandHandler(_packages, _unitOfWork);
        var reactivate = new ReactivatePackageCommandHandler(_packages, _unitOfWork);

        Assert.True((await deactivate.Handle(new DeactivatePackageCommand(package.Id), CancellationToken.None)).IsSuccess);
        Assert.Equal(PackageErrors.AlreadyInactive, (await deactivate.Handle(new DeactivatePackageCommand(package.Id), CancellationToken.None)).Error);
        Assert.True((await reactivate.Handle(new ReactivatePackageCommand(package.Id), CancellationToken.None)).IsSuccess);
        Assert.Equal(PackageErrors.AlreadyActive, (await reactivate.Handle(new ReactivatePackageCommand(package.Id), CancellationToken.None)).Error);
        Assert.Equal(PackageErrors.NotFound, (await deactivate.Handle(new DeactivatePackageCommand(Guid.CreateVersion7()), CancellationToken.None)).Error);
        Assert.Equal(PackageErrors.NotFound, (await reactivate.Handle(new ReactivatePackageCommand(Guid.CreateVersion7()), CancellationToken.None)).Error);
    }

    [Fact]
    public void Validators_ReportEveryInvalidField()
    {
        var invalid = new CreatePackageCommand("", new string('d', 501), 0m, [], 10, 1);

        Assert.Equal(5, new CreatePackageCommandValidator().Validate(invalid).Errors.Count);
        Assert.True(new CreatePackageCommandValidator().Validate(NewPackage()).IsValid);
        Assert.Single(new UpdatePackageCommandValidator().Validate(new UpdatePackageCommand(Guid.Empty, "Revisão", null, 100m, NewPackage().Items, null, null)).Errors);
        Assert.Single(new DeactivatePackageCommandValidator().Validate(new DeactivatePackageCommand(Guid.Empty)).Errors);
        Assert.Single(new ReactivatePackageCommandValidator().Validate(new ReactivatePackageCommand(Guid.Empty)).Errors);
    }
}
