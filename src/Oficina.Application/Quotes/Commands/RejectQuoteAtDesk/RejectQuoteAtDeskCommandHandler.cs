using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Quotes.Decisions;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Commands.RejectQuoteAtDesk;

public sealed class RejectQuoteAtDeskCommandHandler(IEstimateRepository estimates, QuoteDecider decider, QuotePresenter presenter, IUnitOfWork unitOfWork)
    : IRequestHandler<RejectQuoteAtDeskCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(RejectQuoteAtDeskCommand command, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(command.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        if ((await decider.RejectAsync(estimate, command.Reason, cancellationToken)).Error is { } rejectError)
        {
            return rejectError;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await presenter.DtoAsync(estimate, cancellationToken);
    }
}
