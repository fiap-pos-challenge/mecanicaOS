using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.SendQuote;

public sealed record SendQuoteCommand(Guid Id, string Channel, int ExpiresInDays, string Message) : IRequest<Result<SendQuoteResultDto>>;
