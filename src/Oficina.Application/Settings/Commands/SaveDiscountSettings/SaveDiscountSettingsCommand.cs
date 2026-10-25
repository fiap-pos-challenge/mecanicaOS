using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Commands.SaveDiscountSettings;

public sealed record SaveDiscountSettingsCommand(string? Reauthentication, DiscountSettings Input) : IRequest<Result<DiscountSettings>>;
