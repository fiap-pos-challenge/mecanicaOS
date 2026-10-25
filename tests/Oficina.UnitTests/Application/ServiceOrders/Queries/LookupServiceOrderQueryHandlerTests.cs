using NSubstitute;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Application.ServiceOrders.Queries.LookupServiceOrder;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class LookupServiceOrderQueryHandlerTests
{
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly LookupServiceOrderQueryHandler _handler;

    public LookupServiceOrderQueryHandlerTests() =>
        _handler = new LookupServiceOrderQueryHandler(_queries);

    [Fact]
    public async Task Handle_NumberAndDocumentMatch_ReturnsThePublicView()
    {
        var publicView = ServiceOrderTestData.Public();
        _queries.LookupAsync(ServiceOrderNumber.Create(1001).Value, Document.Create(CustomerTestData.Cpf).Value, Arg.Any<CancellationToken>())
            .Returns(publicView);

        var result = await _handler.Handle(new LookupServiceOrderQuery(1001, CustomerTestData.MaskedCpf), CancellationToken.None);

        Assert.Same(publicView, result.Value);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNotFound()
    {
        _queries.LookupAsync(Arg.Any<ServiceOrderNumber>(), Arg.Any<Document>(), Arg.Any<CancellationToken>()).Returns((PublicServiceOrderDto?)null);

        var result = await _handler.Handle(new LookupServiceOrderQuery(1001, CustomerTestData.Cpf), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_InvalidNumber_ReturnsInvalidNumber()
    {
        var result = await _handler.Handle(new LookupServiceOrderQuery(0, CustomerTestData.Cpf), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.InvalidNumber, result.Error);
        await _queries.DidNotReceive().LookupAsync(Arg.Any<ServiceOrderNumber>(), Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDocument_ReturnsInvalidDocument()
    {
        var result = await _handler.Handle(new LookupServiceOrderQuery(1001, "123"), CancellationToken.None);

        Assert.Equal(CustomerErrors.InvalidDocument, result.Error);
        await _queries.DidNotReceive().LookupAsync(Arg.Any<ServiceOrderNumber>(), Arg.Any<Document>(), Arg.Any<CancellationToken>());
    }
}
