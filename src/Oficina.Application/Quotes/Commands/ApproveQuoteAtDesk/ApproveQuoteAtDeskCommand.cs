using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.ApproveQuoteAtDesk;

public sealed record ApproveQuoteAtDeskCommand(Guid Id) : IRequest<Result<QuoteDto>>;
