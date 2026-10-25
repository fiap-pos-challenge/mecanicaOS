using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.SaveQuoteDraft;

public sealed record SaveQuoteDraftCommand(Guid Id, QuoteDraftInput Draft) : IRequest<Result<QuoteDto>>;
