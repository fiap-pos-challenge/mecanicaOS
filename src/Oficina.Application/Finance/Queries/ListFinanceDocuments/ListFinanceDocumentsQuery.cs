using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Finance.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Finance.Queries.ListFinanceDocuments;

public sealed record ListFinanceDocumentsQuery(string? Kind, string? Search, int Page, int PageSize) : IRequest<Result<PagedResult<DocumentSummaryDto>>>;
