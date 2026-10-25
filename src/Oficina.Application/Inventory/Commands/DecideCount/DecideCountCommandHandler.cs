using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.DecideCount;

public sealed class DecideCountCommandHandler(IStockCountRepository counts, IPartRepository parts, IInventoryQueries queries, ICurrentUser currentUser, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<DecideCountCommand, Result<StockCountDto>>
{
    public async Task<Result<StockCountDto>> Handle(DecideCountCommand request, CancellationToken cancellationToken)
    {
        if (await counts.GetByIdAsync(request.Id, cancellationToken) is not { } count)
        {
            return InventoryErrors.CountNotFound;
        }

        var now = timeProvider.GetUtcNow();
        Result decided;
        if (request.Action == "submit")
        {
            decided = count.Submit(now);
        }
        else if (request.Action == "reject")
        {
            decided = count.Reject(request.Reason ?? string.Empty, currentUser.UserId, now);
        }
        else
        {
            var stock = await parts.ListByIdsAsync(count.Lines.Select(line => line.PartId).ToList(), cancellationToken);
            decided = count.Approve(id => stock.FirstOrDefault(part => part.Id == id), currentUser.UserId, now);
        }

        if (decided.Error is { } invalid)
        {
            return invalid;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return (await queries.CountAsync(count.Id, cancellationToken))!;
    }
}
