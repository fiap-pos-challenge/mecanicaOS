using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Converters;

public sealed class EmailConverter() : ValueConverter<Email, string>(email => email.Value, value => Email.Create(value).Value);
