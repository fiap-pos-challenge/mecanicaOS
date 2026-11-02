using NSubstitute;
using Oficina.Application.Customers.Mappers;
using Oficina.Application.Customers.Queries;
using Oficina.Application.Customers.Queries.SearchCustomerByDocument;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application.Customers.Queries;

public sealed class SearchCustomerByDocumentQueryHandlerTests
{
    private readonly ICustomerQueries _queries = Substitute.For<ICustomerQueries>();
    private readonly SearchCustomerByDocumentQueryHandler _handler;

    public SearchCustomerByDocumentQueryHandlerTests() => _handler = new SearchCustomerByDocumentQueryHandler(_queries);

    [Fact]
    public async Task Handle_MaskedDocument_SearchesByTheNormalizedDocument()
    {
        var customer = CustomerTestData.Customer().ToDto();
        _queries.GetByDocumentAsync(Document.Create(CustomerTestData.Cpf).Value, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(new SearchCustomerByDocumentQuery(CustomerTestData.MaskedCpf), CancellationToken.None);

        Assert.Same(customer, result.Value);
    }

    [Fact]
    public async Task Handle_InvalidDocument_ReturnsInvalidDocument()
    {
        var result = await _handler.Handle(new SearchCustomerByDocumentQuery("123"), CancellationToken.None);

        Assert.Equal(CustomerErrors.InvalidDocument, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownDocument_ReturnsNotFound()
    {
        var result = await _handler.Handle(new SearchCustomerByDocumentQuery(CustomerTestData.Cnpj), CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
    }
}
