using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetDiscountSettings;

public sealed record GetDiscountSettingsQuery : IRequest<Result<DiscountSettings>>;
