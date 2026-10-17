using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Requests;

public sealed record ListQuotesRequest(int Page, int PageSize, string? Status) : IRequest<Result<PagedResult<QuoteSummaryDto>>>;

public sealed record ListQuoteServiceOrdersRequest() : IRequest<Result<OpenServiceOrderListDto>>;

public sealed record CreateQuoteRequest(Guid ServiceOrderId) : IRequest<Result<QuoteDto>>;

public sealed record GetQuoteRequest(Guid Id) : IRequest<Result<QuoteDto>>;

public sealed record GetQuoteOptionsRequest(Guid Id) : IRequest<Result<QuoteOptionsDto>>;

public sealed record SaveQuoteDraftRequest(Guid Id, QuoteDraftInput Draft) : IRequest<Result<QuoteDto>>;

public sealed record RequestDiscountApprovalRequest(Guid Id, string Type, decimal Value, string Reason) : IRequest<Result<DiscountApprovalDto>>;

public sealed record DecideDiscountApprovalRequest(Guid Id, Guid ApprovalId, bool Approve) : IRequest<Result<DiscountApprovalDto>>;

public sealed record DraftQuoteMessageRequest(Guid Id) : IRequest<Result<MessageDraftDto>>;

public sealed record SendQuoteRequest(Guid Id, string Channel, int ExpiresInDays, string Message) : IRequest<Result<SendQuoteResultDto>>;

public sealed record GetQuoteDeliveryRequest(Guid Id) : IRequest<Result<QuoteDeliveryDto>>;

public sealed record RetryQuoteDeliveryRequest(Guid Id) : IRequest<Result<QuoteDeliveryDto>>;

public sealed record ApproveQuoteAtDeskRequest(Guid Id) : IRequest<Result<QuoteDto>>;

public sealed record RejectQuoteAtDeskRequest(Guid Id, string Reason) : IRequest<Result<QuoteDto>>;

public sealed record CreateQuoteVersionRequest(Guid Id) : IRequest<Result<QuoteDto>>;

public sealed record ListQuoteVersionsRequest(Guid Id) : IRequest<Result<QuoteVersionListDto>>;
