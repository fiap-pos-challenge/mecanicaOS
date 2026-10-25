using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.RetryQuoteDelivery;

public sealed record RetryQuoteDeliveryCommand(Guid Id) : IRequest<Result<QuoteDeliveryDto>>;
