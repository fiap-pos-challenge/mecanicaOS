using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Domain.ServiceOrders.Services;

public sealed class QuoteApprovalPolicy(IPartRepository parts)
{
    public async Task<Result> ApproveAsync(ServiceOrder serviceOrder, Guid? approvedByUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var approved = serviceOrder.ApproveQuote(approvedByUserId, now);
        return approved.IsFailure ? approved : await DeductApprovedPartsAsync(serviceOrder, approvedByUserId, now, cancellationToken);
    }

    public async Task<Result> ApproveAsync(ServiceOrder serviceOrder, QuoteScope acceptedScope, ApprovalToken approvalToken, Guid? approvedByUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var approved = serviceOrder.ApproveQuote(acceptedScope, approvalToken, approvedByUserId, now);
        return approved.IsFailure ? approved : await DeductApprovedPartsAsync(serviceOrder, approvedByUserId, now, cancellationToken);
    }

    private async Task<Result> DeductApprovedPartsAsync(ServiceOrder serviceOrder, Guid? approvedByUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var partItems in serviceOrder.PartItems.GroupBy(item => item.PartId))
        {
            if (await parts.GetByIdAsync(partItems.Key, cancellationToken) is not { } part)
            {
                return InventoryErrors.InsufficientStock;
            }

            var quantity = Quantity.Create(partItems.Sum(item => item.Quantity.Value)).Value;
            var deducted = part.DeductForServiceOrder(quantity, serviceOrder.Id, serviceOrder.Number.Value, approvedByUserId, now);
            if (deducted.IsFailure)
            {
                return deducted;
            }
        }

        return Result.Success();
    }
}
