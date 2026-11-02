using MediatR;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Customers.Queries.SearchCustomerByDocument;

public sealed record SearchCustomerByDocumentQuery(string Document) : IRequest<Result<CustomerDto>>;
