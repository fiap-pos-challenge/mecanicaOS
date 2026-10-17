using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Catalog.Commands.DeactivateService;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class DeactivateServiceCommandHandlerTests
{
    private readonly IServiceRepository _services = Substitute.For<IServiceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPackageRepository _packages = Substitute.For<IPackageRepository>();
    private readonly DeactivateServiceCommandHandler _handler;

    public DeactivateServiceCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new DeactivateServiceCommandHandler(_services, _packages, _unitOfWork);
        _packages.ListActiveNamesUsingAsync(Arg.Any<CatalogItemKind>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<string>());
    }

    [Fact]
    public async Task Handle_ActiveService_DeactivatesAndCommits()
    {
        var service = Existing(ServiceOrderBuilder.CreateService());

        var result = await _handler.Handle(new DeactivateServiceCommand(service.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(service.IsActive);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ReturnsAlreadyInactiveWithoutCommit()
    {
        var service = Existing(ServiceOrderBuilder.CreateService(active: false));

        var result = await _handler.Handle(new DeactivateServiceCommand(service.Id), CancellationToken.None);

        Assert.Equal(CatalogErrors.AlreadyInactive, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownService_ReturnsNotFoundWithoutCommit()
    {
        var result = await _handler.Handle(new DeactivateServiceCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(CatalogErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceInActivePackage_ReturnsUsedByActivePackageWithoutCommit()
    {
        var service = Existing(ServiceOrderBuilder.CreateService());
        _packages.ListActiveNamesUsingAsync(CatalogItemKind.Service, service.Id, Arg.Any<CancellationToken>()).Returns(new List<string> { "Revisão 10 mil" });

        var result = await _handler.Handle(new DeactivateServiceCommand(service.Id), CancellationToken.None);

        Assert.Equal(CatalogErrors.UsedByActivePackageCode, result.Error!.Code);
        Assert.Contains("Revisão 10 mil", result.Error.Message);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private Service Existing(Service service)
    {
        _services.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        return service;
    }
}
