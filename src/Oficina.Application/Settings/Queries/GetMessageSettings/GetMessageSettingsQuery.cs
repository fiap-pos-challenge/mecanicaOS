using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetMessageSettings;

public sealed record GetMessageSettingsQuery : IRequest<Result<MessageSettings>>;
