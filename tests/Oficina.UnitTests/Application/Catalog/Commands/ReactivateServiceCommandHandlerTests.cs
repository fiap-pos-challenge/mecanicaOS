using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Catalog.Commands.ReactivateService;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class ReactivateServiceCommandHandlerTests
{
    private readonly IServiceRepository _services = Substitute.For<IServiceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ReactivateServiceCommandHandler _handler;

    public ReactivateServiceCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new ReactivateServiceCommandHandler(_services, _unitOfWork);
    }

    [Fact]
    public async Task Handle_InactiveService_ReactivatesAndCommits()
    {
        var service = Existing(ServiceOrderBuilder.CreateService(active: false));

        var result = await _handler.Handle(new ReactivateServiceCommand(service.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(service.IsActive);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyActive_ReturnsAlreadyActiveWithoutCommit()
    {
        var service = Existing(ServiceOrderBuilder.CreateService());

        var result = await _handler.Handle(new ReactivateServiceCommand(service.Id), CancellationToken.None);

        Assert.Equal(CatalogErrors.AlreadyActive, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownService_ReturnsNotFoundWithoutCommit()
    {
        var result = await _handler.Handle(new ReactivateServiceCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(CatalogErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private Service Existing(Service service)
    {
        _services.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        return service;
    }
}
