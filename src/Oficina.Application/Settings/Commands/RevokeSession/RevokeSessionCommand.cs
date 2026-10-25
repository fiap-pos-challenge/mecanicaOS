using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Commands.RevokeSession;

public sealed record RevokeSessionCommand(string? Reauthentication, Guid Id) : IRequest<Result>;
