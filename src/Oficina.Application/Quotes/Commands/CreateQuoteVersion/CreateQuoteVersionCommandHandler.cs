using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Commands.CreateQuoteVersion;

public sealed class CreateQuoteVersionCommandHandler(IEstimateRepository estimates, QuotePresenter presenter, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateQuoteVersionCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(CreateQuoteVersionCommand command, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(command.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        if ((await estimates.GetLatestByNumberAsync(estimate.Number, cancellationToken))?.Id != estimate.Id)
        {
            return QuoteErrors.VersionNotAllowed;
        }

        var next = estimate.NextVersion();
        if (next.Error is { } versionError)
        {
            return versionError;
        }

        estimates.Add(next.Value);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await presenter.DtoAsync(next.Value, cancellationToken);
    }
}
