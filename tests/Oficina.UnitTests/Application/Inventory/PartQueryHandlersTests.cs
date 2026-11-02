using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Mappers;
using Oficina.Application.Inventory.Queries;
using Oficina.Application.Inventory.Queries.GetPartById;
using Oficina.Application.Inventory.Queries.ListParts;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.Errors;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Inventory;

public sealed class PartQueryHandlersTests
{
    private readonly IPartQueries _queries = Substitute.For<IPartQueries>();

    [Fact]
    public async Task GetById_KnownPart_ReturnsTheDetailWithMovements()
    {
        var detail = ServiceOrderBuilder.CreatePart(stock: 10m).ToDetailDto();
        _queries.GetByIdAsync(detail.Id, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await new GetPartByIdQueryHandler(_queries).Handle(new GetPartByIdQuery(detail.Id), CancellationToken.None);

        Assert.Same(detail, result.Value);
        var movement = Assert.Single(result.Value.StockMovements);
        Assert.Equal(StockMovementType.Entry, movement.Type);
        Assert.Equal(10m, movement.BalanceAfter);
    }

    [Fact]
    public async Task GetById_UnknownPart_ReturnsNotFound()
    {
        var result = await new GetPartByIdQueryHandler(_queries).Handle(new GetPartByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(InventoryErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task List_PassesThePageAndFilters()
    {
        var page = new PagedResult<PartDto>(new List<PartDto>(), 2, 10, 0);
        _queries.ListAsync(new PageRequest(2, 10), true, true, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new ListPartsQueryHandler(_queries).Handle(new ListPartsQuery(2, 10, true, true), CancellationToken.None);

        Assert.Same(page, result.Value);
    }
}
