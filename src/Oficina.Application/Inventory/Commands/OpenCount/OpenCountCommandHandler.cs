using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.OpenCount;

public sealed class OpenCountCommandHandler(IStockCountRepository counts, IPartRepository parts, IInventoryQueries queries, ICurrentUser currentUser, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<OpenCountCommand, Result<StockCountDto>>
{
    public async Task<Result<StockCountDto>> Handle(OpenCountCommand request, CancellationToken cancellationToken)
    {
        var location = request.Location.Trim().ToUpperInvariant();
        var inLocation = await parts.ListActiveInLocationAsync(location, cancellationToken);
        if (inLocation.Count == 0)
        {
            return InventoryErrors.UnknownLocation;
        }

        if (await counts.HasOpenAsync(location, cancellationToken))
        {
            return InventoryErrors.CountAlreadyOpen;
        }

        var count = StockCount.Open(location, inLocation, currentUser.UserId, timeProvider.GetUtcNow());
        counts.Add(count);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return (await queries.CountAsync(count.Id, cancellationToken))!;
    }
}
