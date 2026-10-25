using MediatR;
using Oficina.Application.Quotes.Delivery;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.SendQuote;

public sealed class SendQuoteCommandHandler(QuoteDispatcher dispatcher) : IRequestHandler<SendQuoteCommand, Result<SendQuoteResultDto>>
{
    public Task<Result<SendQuoteResultDto>> Handle(SendQuoteCommand command, CancellationToken cancellationToken) =>
        dispatcher.SendAsync(command.Id, command.Channel, command.ExpiresInDays, command.Message, cancellationToken);
}
