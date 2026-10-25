using MediatR;
using Oficina.Application.Settings.Dtos;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Commands.SaveWorkshopSettings;

public sealed record SaveWorkshopSettingsCommand(string? Reauthentication, WorkshopInput Input) : IRequest<Result<WorkshopSettings>>;
