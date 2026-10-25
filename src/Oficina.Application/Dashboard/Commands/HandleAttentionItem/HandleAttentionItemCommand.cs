using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Dashboard.Commands.HandleAttentionItem;

public sealed record HandleAttentionItemCommand(string Id, string? Action) : IRequest<Result>;
