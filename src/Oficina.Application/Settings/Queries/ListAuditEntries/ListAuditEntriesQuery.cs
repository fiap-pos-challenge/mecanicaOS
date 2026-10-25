using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Settings.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.ListAuditEntries;

public sealed record ListAuditEntriesQuery(AuditFilter Filter) : IRequest<Result<PagedResult<AuditEntryDto>>>;
