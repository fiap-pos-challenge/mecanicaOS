using MediatR;
using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Links;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Documents.Dtos;
using Oficina.Application.Quotes.Delivery;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Portal.Errors;

namespace Oficina.Application.Documents.Commands.SendDocument;

public sealed partial class SendDocumentCommandHandler(
    IDocumentRepository documents,
    IQuoteQueries quoteQueries,
    IPublicLinks links,
    IEmailSender emailSender,
    ILogger<SendDocumentCommandHandler> logger) : IRequestHandler<SendDocumentCommand, Result<DocumentSendDto>>
{
    public async Task<Result<DocumentSendDto>> Handle(SendDocumentCommand command, CancellationToken cancellationToken)
    {
        if (command.Channel is not (QuoteChannel.WhatsApp or QuoteChannel.Email))
        {
            return PortalErrors.InvalidChannel;
        }

        if (await documents.GetByCodeAsync(command.Code, cancellationToken) is not { } document)
        {
            return PortalErrors.DocumentNotFound;
        }

        var context = await quoteQueries.ContextAsync(document.ServiceOrderId, cancellationToken);
        if (command.Channel == QuoteChannel.Email && context is { ConsentEmail: true, Email: { } email })
        {
            var sent = await emailSender.SendAsync(new EmailMessage(email, DocumentTexts.EmailSubject(document.Snapshot.ShopName, document.Code),
                DocumentTexts.EmailBody(context.CustomerName.Split(' ')[0], document.Code, links.DocumentVerification(document.Code))), cancellationToken);
            return new DocumentSendDto(sent ? DocumentSendDto.Sent : DocumentSendDto.Failed);
        }

        LogDocumentQueued(logger, command.Channel);
        return new DocumentSendDto(DocumentSendDto.Queued);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Document queued for {Channel}")]
    private static partial void LogDocumentQueued(ILogger logger, string channel);
}
