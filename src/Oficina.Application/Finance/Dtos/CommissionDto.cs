namespace Oficina.Application.Finance.Dtos;

public sealed record CommissionDto(string MechanicName, int Orders, long Services, long Commission);
