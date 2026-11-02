using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Commands;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class ServiceOrderChangesTests
{
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ServiceOrder _order = new ServiceOrderBuilder().Build();
    private readonly ServiceOrderChanges _changes;

    public ServiceOrderChangesTests()
    {
        _serviceOrders.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(_order);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _changes = new ServiceOrderChanges(_serviceOrders, _queries, _unitOfWork);
    }

    [Fact]
    public async Task ApplyAsync_SuccessfulChange_CommitsAndReturnsTheDetail()
    {
        var detail = ServiceOrderTestData.Detail(_order.Id);
        _queries.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await _changes.ApplyAsync(_order.Id, _ => Result.Success(), CancellationToken.None);

        Assert.Same(detail, result.Value);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_UnknownServiceOrder_ReturnsNotFoundWithoutCallingTheChange()
    {
        var called = false;

        var result = await _changes.ApplyAsync(Guid.CreateVersion7(), _ => { called = true; return Result.Success(); }, CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
        Assert.False(called);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_ChangeFails_ReturnsTheErrorWithoutCommit()
    {
        var result = await _changes.ApplyAsync(_order.Id, _ => ServiceOrderErrors.InvalidTransition, CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.InvalidTransition, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _changes.ApplyAsync(_order.Id, _ => Result.Success(), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
        await _queries.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_DetailMissingAfterCommit_ReturnsNotFound()
    {
        _queries.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns((ServiceOrderDetailDto?)null);

        var result = await _changes.ApplyAsync(_order.Id, _ => Result.Success(), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
    }
}
