using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Converters;

public sealed class MoneyConverter() : ValueConverter<Money, decimal>(money => money.Amount, amount => Money.Create(amount).Value);
