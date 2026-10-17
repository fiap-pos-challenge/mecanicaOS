namespace Oficina.Application.Settings.Dtos;

public sealed record LogoInput(string FileName, string ContentType, string DataUrl);

public sealed record LogoDto(string LogoUrl);

public sealed record SessionDto(Guid Id, string UserName, string Device, DateTimeOffset LastSeenAt, bool Current);

public sealed record SessionListDto(IReadOnlyList<SessionDto> Items);

public sealed record AuditEntryDto(Guid Id, DateTimeOffset OccurredAt, string ActorName, string Area, string Action, string Target, string Detail);

public sealed record AuditFilter(string? Area, DateOnly? From, DateOnly? To, string? Actor, int Page, int PageSize);

public sealed record WorkshopInput(string Name, string Document, Models.WorkshopAddressSettings Address, IReadOnlyList<Models.BusinessHoursSettings> Hours);

public sealed record PaymentInput(IReadOnlyList<string> Methods, int MaxInstallments, int InterestFreeInstallments, decimal MonthlyInterestPercent, IReadOnlyList<Models.PaymentFeeSettings> Fees, string? PixKey);
