using Oficina.Domain.Privacy.Enums;

namespace Oficina.Api.Contracts.Customers;

public sealed record RevealCustomerDataRequest(SensitiveField Field, AccessReason Reason);
