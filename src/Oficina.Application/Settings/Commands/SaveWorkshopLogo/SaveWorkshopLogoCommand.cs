using MediatR;
using Oficina.Application.Settings.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Commands.SaveWorkshopLogo;

public sealed record SaveWorkshopLogoCommand(string? Reauthentication, LogoInput Input) : IRequest<Result<LogoDto>>;
