using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.CreateQuote;

public sealed record CreateQuoteCommand(Guid ServiceOrderId) : IRequest<Result<QuoteDto>>;
