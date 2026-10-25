using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.DraftQuoteMessage;

public sealed record DraftQuoteMessageQuery(Guid Id) : IRequest<Result<MessageDraftDto>>;
