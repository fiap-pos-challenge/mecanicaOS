using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Converters;

public sealed class QuantityConverter() : ValueConverter<Quantity, decimal>(quantity => quantity.Value, value => Quantity.Create(value).Value);
