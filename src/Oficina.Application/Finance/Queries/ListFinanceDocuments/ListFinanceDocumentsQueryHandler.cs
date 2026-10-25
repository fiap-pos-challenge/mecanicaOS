using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Finance.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Finance.Queries.ListFinanceDocuments;

public sealed class ListFinanceDocumentsQueryHandler(IFinanceQueries queries) : IRequestHandler<ListFinanceDocumentsQuery, Result<PagedResult<DocumentSummaryDto>>>
{
    private static readonly string[] Kinds = ["ServiceOrder", "Quote", "Receipt"];

    public async Task<Result<PagedResult<DocumentSummaryDto>>> Handle(ListFinanceDocumentsQuery query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.Kind) && !Kinds.Contains(query.Kind))
        {
            return FinanceErrors.InvalidKind;
        }

        var size = query.PageSize is < 1 or > PageRequest.MaxPageSize ? PageRequest.DefaultPageSize : query.PageSize;
        return await queries.DocumentsAsync(string.IsNullOrWhiteSpace(query.Kind) ? null : query.Kind, query.Search?.Trim(), Math.Max(PageRequest.FirstPage, query.Page), size, cancellationToken);
    }
}
