using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetWorkshopSettings;

public sealed record GetWorkshopSettingsQuery : IRequest<Result<WorkshopSettings>>;
