namespace Oficina.Application.Documents.Dtos;

public sealed record DocumentVerificationDto(bool Valid, string? Kind, long? Number, DateTimeOffset? IssuedAt, decimal? Total, string? ShopName);
