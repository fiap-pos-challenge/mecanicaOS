using MediatR;
using Oficina.Application.Addresses.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Addresses.Queries.LookupAddress;

public sealed record LookupAddressQuery(string ZipCode) : IRequest<Result<AddressLookupDto>>;
