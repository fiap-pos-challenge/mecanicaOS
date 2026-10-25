using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class ServiceOrderPhoto : AggregateRoot
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

    public static Result<ServiceOrderPhoto> Take(ServiceOrder serviceOrder, string? clientId, string contentType, string dataUrl, string caption, DateTimeOffset now)
    {
        if (!AllowedContentTypes.Contains(contentType) || !dataUrl.StartsWith($"data:{contentType}", StringComparison.Ordinal))
        {
            return ServiceOrderErrors.InvalidPhotoType;
        }

        if (dataUrl.Length > MaxDataUrlLength)
        {
            return ServiceOrderErrors.PhotoTooLarge;
        }

        var registered = serviceOrder.RegisterPhoto();
        if (registered.Error is { } limitError)
        {
            return limitError;
        }

        var trimmed = caption.Trim();
        return new ServiceOrderPhoto(Guid.CreateVersion7(), serviceOrder.Id, clientId, contentType, dataUrl, trimmed.Length > CaptionMaxLength ? trimmed[..CaptionMaxLength] : trimmed, now);
    }
}
