using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Commands.SaveMessageSettings;

public sealed record SaveMessageSettingsCommand(string? Reauthentication, MessageSettings Input) : IRequest<Result<MessageSettings>>;
