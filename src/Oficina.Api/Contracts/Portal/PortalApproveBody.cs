namespace Oficina.Api.Contracts.Portal;

public sealed record PortalApproveBody(IReadOnlyList<Guid>? ItemIds);
