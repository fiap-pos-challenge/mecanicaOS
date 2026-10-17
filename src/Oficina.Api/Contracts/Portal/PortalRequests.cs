namespace Oficina.Api.Contracts.Portal;

public sealed record PortalTokenBody(string Token);

public sealed record PortalConfirmBody(string Token, string LastDigits);

public sealed record PortalApproveBody(IReadOnlyList<Guid>? ItemIds);

public sealed record PortalRejectBody(string Reason);

public sealed record PixWebhookBody(string TxId);

public sealed record SendDocumentBody(string Channel);
