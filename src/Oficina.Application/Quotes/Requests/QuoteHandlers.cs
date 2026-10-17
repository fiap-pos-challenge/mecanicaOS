using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Requests;

public sealed class ListQuotesHandler(QuoteWorkflow workflow) : IRequestHandler<ListQuotesRequest, Result<PagedResult<QuoteSummaryDto>>>
{
    public Task<Result<PagedResult<QuoteSummaryDto>>> Handle(ListQuotesRequest request, CancellationToken cancellationToken) => workflow.ListAsync(request.Page, request.PageSize, request.Status, cancellationToken);
}

public sealed class ListQuoteServiceOrdersHandler(QuoteWorkflow workflow) : IRequestHandler<ListQuoteServiceOrdersRequest, Result<OpenServiceOrderListDto>>
{
    public Task<Result<OpenServiceOrderListDto>> Handle(ListQuoteServiceOrdersRequest request, CancellationToken cancellationToken) => workflow.OpenServiceOrdersAsync(cancellationToken);
}

public sealed class CreateQuoteHandler(QuoteWorkflow workflow) : IRequestHandler<CreateQuoteRequest, Result<QuoteDto>>
{
    public Task<Result<QuoteDto>> Handle(CreateQuoteRequest request, CancellationToken cancellationToken) => workflow.CreateAsync(request.ServiceOrderId, cancellationToken);
}

public sealed class GetQuoteHandler(QuoteWorkflow workflow) : IRequestHandler<GetQuoteRequest, Result<QuoteDto>>
{
    public Task<Result<QuoteDto>> Handle(GetQuoteRequest request, CancellationToken cancellationToken) => workflow.GetAsync(request.Id, cancellationToken);
}

public sealed class GetQuoteOptionsHandler(QuoteWorkflow workflow) : IRequestHandler<GetQuoteOptionsRequest, Result<QuoteOptionsDto>>
{
    public Task<Result<QuoteOptionsDto>> Handle(GetQuoteOptionsRequest request, CancellationToken cancellationToken) => workflow.OptionsAsync(request.Id, cancellationToken);
}

public sealed class SaveQuoteDraftHandler(QuoteWorkflow workflow) : IRequestHandler<SaveQuoteDraftRequest, Result<QuoteDto>>
{
    public Task<Result<QuoteDto>> Handle(SaveQuoteDraftRequest request, CancellationToken cancellationToken) => workflow.SaveDraftAsync(request.Id, request.Draft, cancellationToken);
}

public sealed class RequestDiscountApprovalHandler(QuoteWorkflow workflow) : IRequestHandler<RequestDiscountApprovalRequest, Result<DiscountApprovalDto>>
{
    public Task<Result<DiscountApprovalDto>> Handle(RequestDiscountApprovalRequest request, CancellationToken cancellationToken) => workflow.RequestApprovalAsync(request.Id, request.Type, request.Value, request.Reason, cancellationToken);
}

public sealed class DecideDiscountApprovalHandler(QuoteWorkflow workflow) : IRequestHandler<DecideDiscountApprovalRequest, Result<DiscountApprovalDto>>
{
    public Task<Result<DiscountApprovalDto>> Handle(DecideDiscountApprovalRequest request, CancellationToken cancellationToken) => workflow.DecideApprovalAsync(request.Id, request.ApprovalId, request.Approve, cancellationToken);
}

public sealed class DraftQuoteMessageHandler(QuoteWorkflow workflow) : IRequestHandler<DraftQuoteMessageRequest, Result<MessageDraftDto>>
{
    public Task<Result<MessageDraftDto>> Handle(DraftQuoteMessageRequest request, CancellationToken cancellationToken) => workflow.MessageDraftAsync(request.Id, cancellationToken);
}

public sealed class SendQuoteHandler(QuoteWorkflow workflow) : IRequestHandler<SendQuoteRequest, Result<SendQuoteResultDto>>
{
    public Task<Result<SendQuoteResultDto>> Handle(SendQuoteRequest request, CancellationToken cancellationToken) => workflow.SendAsync(request.Id, request.Channel, request.ExpiresInDays, request.Message, cancellationToken);
}

public sealed class GetQuoteDeliveryHandler(QuoteWorkflow workflow) : IRequestHandler<GetQuoteDeliveryRequest, Result<QuoteDeliveryDto>>
{
    public Task<Result<QuoteDeliveryDto>> Handle(GetQuoteDeliveryRequest request, CancellationToken cancellationToken) => workflow.DeliveryAsync(request.Id, cancellationToken);
}

public sealed class RetryQuoteDeliveryHandler(QuoteWorkflow workflow) : IRequestHandler<RetryQuoteDeliveryRequest, Result<QuoteDeliveryDto>>
{
    public Task<Result<QuoteDeliveryDto>> Handle(RetryQuoteDeliveryRequest request, CancellationToken cancellationToken) => workflow.RetryAsync(request.Id, cancellationToken);
}

public sealed class ApproveQuoteAtDeskHandler(QuoteWorkflow workflow) : IRequestHandler<ApproveQuoteAtDeskRequest, Result<QuoteDto>>
{
    public Task<Result<QuoteDto>> Handle(ApproveQuoteAtDeskRequest request, CancellationToken cancellationToken) => workflow.ApproveAsync(request.Id, null, cancellationToken);
}

public sealed class RejectQuoteAtDeskHandler(QuoteWorkflow workflow) : IRequestHandler<RejectQuoteAtDeskRequest, Result<QuoteDto>>
{
    public Task<Result<QuoteDto>> Handle(RejectQuoteAtDeskRequest request, CancellationToken cancellationToken) => workflow.RejectAsync(request.Id, request.Reason, cancellationToken);
}

public sealed class CreateQuoteVersionHandler(QuoteWorkflow workflow) : IRequestHandler<CreateQuoteVersionRequest, Result<QuoteDto>>
{
    public Task<Result<QuoteDto>> Handle(CreateQuoteVersionRequest request, CancellationToken cancellationToken) => workflow.NewVersionAsync(request.Id, cancellationToken);
}

public sealed class ListQuoteVersionsHandler(QuoteWorkflow workflow) : IRequestHandler<ListQuoteVersionsRequest, Result<QuoteVersionListDto>>
{
    public Task<Result<QuoteVersionListDto>> Handle(ListQuoteVersionsRequest request, CancellationToken cancellationToken) => workflow.VersionsAsync(request.Id, cancellationToken);
}
