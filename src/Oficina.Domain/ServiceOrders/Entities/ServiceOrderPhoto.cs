using Oficina.Domain.Common.Entities;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class ServiceOrderPhoto : Entity
{
    public const int CaptionMaxLength = 160;
    public const int ClientIdMaxLength = 64;
    public const int MaxDataUrlLength = 7_000_000;

    public static readonly IReadOnlyList<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private ServiceOrderPhoto(Guid id, Guid serviceOrderId, string? clientId, string contentType, string dataUrl, string caption, DateTimeOffset takenAt)
        : base(id)
    {
        ServiceOrderId = serviceOrderId;
        ClientId = clientId;
        ContentType = contentType;
        DataUrl = dataUrl;
        Caption = caption;
        TakenAt = takenAt;
    }

    public Guid ServiceOrderId { get; private set; }

    public string? ClientId { get; private set; }

    public string ContentType { get; private set; }

    public string DataUrl { get; private set; }

    public string Caption { get; private set; }

    public DateTimeOffset TakenAt { get; private set; }

    internal static ServiceOrderPhoto Create(Guid serviceOrderId, string? clientId, string contentType, string dataUrl, string caption, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), serviceOrderId, clientId, contentType, dataUrl, caption.Length > CaptionMaxLength ? caption[..CaptionMaxLength] : caption, now);
}
