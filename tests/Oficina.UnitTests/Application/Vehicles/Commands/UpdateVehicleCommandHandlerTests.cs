using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Vehicles.Commands.UpdateVehicle;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.UnitTests.Application.Vehicles.Commands;

public sealed class UpdateVehicleCommandHandlerTests
{
    private readonly IVehicleRepository _repository = Substitute.For<IVehicleRepository>();
    private readonly UpdateVehicleCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.Parse("2026-06-15T00:00:00Z"));
    private readonly Vehicle _vehicle = VehicleTestData.Vehicle();

    public UpdateVehicleCommandHandlerTests()
    {
        _repository.GetByIdAsync(_vehicle.Id, Arg.Any<CancellationToken>()).Returns(_vehicle);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new UpdateVehicleCommandHandler(_repository, _unitOfWork, _timeProvider);
    }

    [Fact]
    public async Task Handle_ValidCommand_ChangeDetailsAndCommit()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("HB20", result.Value.Model);
        Assert.Equal("Hyundai", result.Value.Brand);
        Assert.Equal(2025,  result.Value.Year);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownVehicle_ReturnsNotFoundWithoutCommit()
    {
        var result = await _handler.Handle(Command() with { Id = Guid.CreateVersion7() }, CancellationToken.None);

        Assert.Equal(VehicleErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidData_ReturnsTheDomainErrorWithoutCommit()
    {
        var result = await _handler.Handle(Command() with { Model = "Hyundai", Brand = "HB20", Year = 1800}, CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidYear.Code, result.Error!.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_YearAfterNextYear_ReturnsInvalidYearWithoutCommit()
    {
        var farFutureYear = _timeProvider.GetUtcNow().Year + 2;

        var result = await _handler.Handle(Command() with { Year = farFutureYear }, CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidYear, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    public UpdateVehicleCommand Command() =>
        new(_vehicle.Id, "HB20", "Hyundai", 2025, 50_000);
}
