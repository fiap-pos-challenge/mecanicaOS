using MediatR;
using Oficina.Application.Settings.Dtos;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Commands.SavePaymentSettings;

public sealed record SavePaymentSettingsCommand(string? Reauthentication, PaymentInput Input) : IRequest<Result<PaymentSettings>>;
