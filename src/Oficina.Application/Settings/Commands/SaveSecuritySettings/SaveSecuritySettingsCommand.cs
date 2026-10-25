using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Commands.SaveSecuritySettings;

public sealed record SaveSecuritySettingsCommand(string? Reauthentication, SecuritySettings Input) : IRequest<Result<SecuritySettings>>;
