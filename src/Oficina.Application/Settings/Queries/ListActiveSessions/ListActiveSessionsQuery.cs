using MediatR;
using Oficina.Application.Settings.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.ListActiveSessions;

public sealed record ListActiveSessionsQuery : IRequest<Result<SessionListDto>>;
