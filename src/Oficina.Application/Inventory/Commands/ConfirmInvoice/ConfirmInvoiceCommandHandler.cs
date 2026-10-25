using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.ConfirmInvoice;

public sealed class ConfirmInvoiceCommandHandler(IPartRepository parts, ICurrentUser currentUser, IUnitOfWork unitOfWork) : IRequestHandler<ConfirmInvoiceCommand, Result<InvoiceConfirmedDto>>
{
    public async Task<Result<InvoiceConfirmedDto>> Handle(ConfirmInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            return InventoryErrors.InvalidInvoiceLines;
        }

        var reason = $"Nota {request.InvoiceNumber.Trim()}";
        foreach (var line in request.Lines)
        {
            var quantity = Quantity.Create(line.Quantity);
            var cost = Money.Create(line.UnitCost);
            if (await parts.GetByIdAsync(line.PartId, cancellationToken) is not { } part || quantity.IsFailure || cost.IsFailure
                || part.ReceiveStock(quantity.Value, cost.Value, reason, currentUser.UserId).IsFailure)
            {
                return InventoryErrors.InvalidInvoiceLines;
            }
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new InvoiceConfirmedDto(request.Lines.Count);
    }
}
