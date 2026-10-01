using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Converters;

public sealed class ApprovalTokenConverter() : ValueConverter<ApprovalToken, string>(token => token.Hash, hash => ApprovalToken.Create(hash).Value);
