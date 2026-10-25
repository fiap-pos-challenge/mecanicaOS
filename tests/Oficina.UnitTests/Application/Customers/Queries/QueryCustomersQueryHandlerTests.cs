using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Queries;
using Oficina.Application.Customers.Queries.QueryCustomers;
using Oficina.Domain.Customers.Enums;

namespace Oficina.UnitTests.Application.Customers.Queries;

public sealed class QueryCustomersQueryHandlerTests
{
    private readonly ICustomerQueries _queries = Substitute.For<ICustomerQueries>();
    private readonly QueryCustomersQueryHandler _handler;

    public QueryCustomersQueryHandlerTests()
    {
        _queries.QueryAsync(Arg.Any<CustomerFilter>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CustomerDto>(Array.Empty<CustomerDto>(), 1, 20, 0));
        _handler = new QueryCustomersQueryHandler(_queries);
    }

    [Fact]
    public async Task Handle_FullDocument_SearchesByDocument()
    {
        await _handler.Handle(new QueryCustomersQuery(" 529.982.247-25 ", DocumentType.Cpf, CustomerSort.Name, SortDirection.Asc, 1, 20), CancellationToken.None);

        await _queries.Received(1).QueryAsync(
            Arg.Is<CustomerFilter>(filter => filter.Name == null && filter.Document!.Value == CustomerTestData.Cpf && filter.DocumentType == DocumentType.Cpf),
            new PageRequest(1, 20),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(" Maria ", "Maria")]
    [InlineData("529982", "529982")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public async Task Handle_OtherTerms_SearchByName(string? term, string? expectedName)
    {
        await _handler.Handle(new QueryCustomersQuery(term, null, CustomerSort.CreatedAt, SortDirection.Desc, 2, 10), CancellationToken.None);

        await _queries.Received(1).QueryAsync(
            Arg.Is<CustomerFilter>(filter => filter.Name == expectedName && filter.Document == null && filter.Sort == CustomerSort.CreatedAt && filter.Direction == SortDirection.Desc),
            new PageRequest(2, 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_RejectsLongTermsBadPagingAndUnknownEnums()
    {
        var validator = new QueryCustomersQueryValidator();

        Assert.True(validator.Validate(new QueryCustomersQuery("Maria", DocumentType.Cnpj, CustomerSort.Name, SortDirection.Asc, 1, 20)).IsValid);
        Assert.False(validator.Validate(new QueryCustomersQuery(new string('a', 161), null, CustomerSort.Name, SortDirection.Asc, 1, 20)).IsValid);
        Assert.False(validator.Validate(new QueryCustomersQuery(null, null, CustomerSort.Name, SortDirection.Asc, 0, 20)).IsValid);
        Assert.False(validator.Validate(new QueryCustomersQuery(null, null, CustomerSort.Name, SortDirection.Asc, 1, 101)).IsValid);
        Assert.False(validator.Validate(new QueryCustomersQuery(null, (DocumentType)7, (CustomerSort)7, (SortDirection)7, 1, 20)).IsValid);
    }
}
