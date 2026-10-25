using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Settings.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Settings.Errors;

namespace Oficina.Application.Settings.Queries.ListAuditEntries;

public sealed class ListAuditEntriesQueryHandler(ISettingsQueries queries) : IRequestHandler<ListAuditEntriesQuery, Result<PagedResult<AuditEntryDto>>>
{
    public async Task<Result<PagedResult<AuditEntryDto>>> Handle(ListAuditEntriesQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Filter;
        var area = string.IsNullOrWhiteSpace(filter.Area) || filter.Area == "all" ? null : filter.Area;
        if (area is not null && !SettingsRules.AuditAreas.Contains(area))
        {
            return SettingsErrors.Invalid("area", SettingsMessages.Area);
        }

        var page = Math.Max(PageRequest.FirstPage, filter.Page);
        var size = filter.PageSize is < 1 or > PageRequest.MaxPageSize ? PageRequest.DefaultPageSize : filter.PageSize;
        return await queries.AuditAsync(filter with { Area = area, Page = page, PageSize = size, Actor = filter.Actor?.Trim() }, cancellationToken);
    }
}
