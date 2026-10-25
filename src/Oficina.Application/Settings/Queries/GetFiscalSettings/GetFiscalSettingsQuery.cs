using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetFiscalSettings;

public sealed record GetFiscalSettingsQuery : IRequest<Result<FiscalSettings>>;
