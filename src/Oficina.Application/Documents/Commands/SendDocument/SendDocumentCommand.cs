using MediatR;
using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Documents.Commands.SendDocument;

public sealed record SendDocumentCommand(string Code, string? Channel) : IRequest<Result<DocumentSendDto>>;
