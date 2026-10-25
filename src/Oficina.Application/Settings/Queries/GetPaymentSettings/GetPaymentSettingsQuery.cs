using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetPaymentSettings;

public sealed record GetPaymentSettingsQuery : IRequest<Result<PaymentSettings>>;
