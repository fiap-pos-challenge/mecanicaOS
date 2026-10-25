using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetSecuritySettings;

public sealed record GetSecuritySettingsQuery : IRequest<Result<SecuritySettings>>;
