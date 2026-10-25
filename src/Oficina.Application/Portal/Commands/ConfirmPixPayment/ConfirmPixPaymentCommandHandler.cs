using MediatR;
using Oficina.Application.Abstractions.Payments;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Portal.Repositories;

namespace Oficina.Application.Portal.Commands.ConfirmPixPayment;

public sealed class ConfirmPixPaymentCommandHandler(IPortalRepository portal, IPixWebhookVerifier verifier, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<ConfirmPixPaymentCommand, Result>
{
    public async Task<Result> Handle(ConfirmPixPaymentCommand command, CancellationToken cancellationToken)
    {
        var txId = command.TxId ?? string.Empty;
        if (!verifier.IsValid(txId, command.Signature ?? string.Empty))
        {
            return PortalErrors.InvalidWebhook;
        }

        if (await portal.GetChargeByTxIdAsync(txId, cancellationToken) is not { } charge)
        {
            return PortalErrors.PaymentNotFound;
        }

        charge.MarkPaid(timeProvider.GetUtcNow());
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
