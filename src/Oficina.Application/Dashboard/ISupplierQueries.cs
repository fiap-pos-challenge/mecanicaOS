namespace Oficina.Application.Dashboard;

public interface ISupplierQueries
{
    Task<Guid?> PreferredAsync(string? name, CancellationToken cancellationToken);
}
