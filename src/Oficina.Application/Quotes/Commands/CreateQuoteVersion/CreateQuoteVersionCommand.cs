using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Commands.CreateQuoteVersion;

public sealed record CreateQuoteVersionCommand(Guid Id) : IRequest<Result<QuoteDto>>;
