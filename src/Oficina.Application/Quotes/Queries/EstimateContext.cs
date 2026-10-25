using Oficina.Domain.Quotes.Pricing;

namespace Oficina.Application.Quotes.Queries;

public sealed record EstimateContext(
    long ServiceOrderNumber,
    Guid CustomerId,
    string CustomerName,
    string? Phone,
    string? Email,
    bool ConsentWhatsApp,
    bool ConsentEmail,
    CustomerFacts Facts,
    string LicensePlate,
    string Vehicle,
    int Mileage,
    string? Diagnosis);
