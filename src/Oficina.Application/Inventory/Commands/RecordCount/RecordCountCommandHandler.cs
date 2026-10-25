using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.RecordCount;

public sealed class RecordCountCommandHandler(IStockCountRepository counts, IPartRepository parts, IInventoryQueries queries, IUnitOfWork unitOfWork) : IRequestHandler<RecordCountCommand, Result<StockCountDto>>
{
    public async Task<Result<StockCountDto>> Handle(RecordCountCommand request, CancellationToken cancellationToken)
    {
        if (await counts.GetByIdAsync(request.Id, cancellationToken) is not { } count)
        {
            return InventoryErrors.CountNotFound;
        }

        var stock = await parts.ListByIdsAsync(count.Lines.Select(line => line.PartId).ToList(), cancellationToken);
        var recorded = count.Record(request.Lines.Select(line => (line.PartId, line.CountedQuantity)).ToList(), id => stock.FirstOrDefault(part => part.Id == id));
        if (recorded.Error is { } invalid)
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
