using Oficina.Application.ServiceOrders.Dtos;

namespace Oficina.Application.ServiceOrders.Queries;

public interface IServiceOrderBoardQueries
{
    Task<MechanicOrderDto?> GetWorkAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MechanicOrderDto>> ListMineAsync(Guid mechanicId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SuggestedItemDto>> SuggestAsync(string description, CancellationToken cancellationToken);
}
