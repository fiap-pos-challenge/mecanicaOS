using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.RejectQuoteAtDesk;

public sealed record RejectQuoteAtDeskCommand(Guid Id, string Reason) : IRequest<Result<QuoteDto>>;
