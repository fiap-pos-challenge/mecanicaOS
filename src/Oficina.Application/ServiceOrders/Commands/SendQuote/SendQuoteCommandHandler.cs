using System.Security.Cryptography;
using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.ServiceOrders.Commands.SendQuote;

public sealed class SendQuoteCommandHandler(ServiceOrderChanges changes, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<SendQuoteCommand, Result<SentQuoteDto>>
{
    private const int TokenBytes = 32;

    public async Task<Result<SentQuoteDto>> Handle(SendQuoteCommand command, CancellationToken cancellationToken)
    {
        var rawToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(TokenBytes));
        var approvalToken = ApprovalToken.FromRawToken(rawToken).Value;

        var sent = await changes.ApplyAsync(
            command.ServiceOrderId,
            serviceOrder => serviceOrder.SendQuote(approvalToken, currentUser.UserId, timeProvider.GetUtcNow()),
            cancellationToken);
        if (sent.Error is { } error)
        {
            return error;
        }

        return new SentQuoteDto(rawToken, sent.Value);
    }
}
