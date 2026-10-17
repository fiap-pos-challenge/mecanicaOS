using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.ServiceOrders.Queries.LookupServiceOrder;

public sealed class LookupServiceOrderQueryHandler(IServiceOrderQueries queries)
    : IRequestHandler<LookupServiceOrderQuery, Result<PublicServiceOrderDto>>
{
    public async Task<Result<PublicServiceOrderDto>> Handle(LookupServiceOrderQuery query, CancellationToken cancellationToken)
    {
        var number = ServiceOrderNumber.Create(query.Number);
        var document = Document.Create(query.Document);
        if ((number.Error ?? document.Error) is { } error)
        {
            return error;
        }

        if (await queries.LookupAsync(number.Value, document.Value, cancellationToken) is not { } serviceOrder)
        {
            return ServiceOrderErrors.NotFound;
        }

        return serviceOrder;
    }
}
