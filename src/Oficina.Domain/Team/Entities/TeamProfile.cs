using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Team.Errors;

namespace Oficina.Domain.Team.Entities;

public sealed class TeamProfile
{
    public const int MaxSpecialties = 8;
    public const int SpecialtyMinLength = 2;
    public const int SpecialtyMaxLength = 40;
    public const decimal MaxCommissionPercent = 50m;
    public const decimal MaxDiscountLimitPercent = 30m;

    private TeamProfile(Guid userId, List<string> specialties, decimal commissionPercent, decimal discountLimitPercent, DateTimeOffset? updatedAt)
    {
        UserId = userId;
        Specialties = specialties;
        CommissionPercent = commissionPercent;
        DiscountLimitPercent = discountLimitPercent;
        Schedule = [];
        UpdatedAt = updatedAt;
    }

    public Guid UserId { get; private init; }

    public List<string> Specialties { get; private set; }

    public decimal CommissionPercent { get; private set; }

    public decimal DiscountLimitPercent { get; private set; }

    public List<WorkShift> Schedule { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static TeamProfile Empty(Guid userId) => new(userId, [], 0m, 0m, null);

    public Result Change(Role role, IReadOnlyCollection<string> specialties, decimal commissionPercent, decimal discountLimitPercent, IReadOnlyCollection<WorkShift> schedule, DateTimeOffset now)
    {
        var trimmed = specialties.Select(specialty => specialty.Trim()).ToList();
        if (trimmed.Count > MaxSpecialties || trimmed.Any(specialty => specialty.Length is < SpecialtyMinLength or > SpecialtyMaxLength))
        {
            return TeamErrors.InvalidSpecialties;
        }

        if (commissionPercent is < 0 or > MaxCommissionPercent)
        {
            return TeamErrors.InvalidCommission;
        }

        if (discountLimitPercent is < 0 or > MaxDiscountLimitPercent)
        {
            return TeamErrors.InvalidDiscountLimit;
        }

        if (schedule.Any(shift => shift.Start >= shift.End) || schedule.Select(shift => shift.Day).Distinct().Count() != schedule.Count)
        {
            return TeamErrors.InvalidShift;
        }

        if (role == Role.Mechanic && discountLimitPercent > 0)
        {
            return TeamErrors.MechanicDiscount;
        }

        Specialties = trimmed;
        CommissionPercent = commissionPercent;
        DiscountLimitPercent = discountLimitPercent;
        Schedule = schedule.OrderBy(shift => ((int)shift.Day + 6) % 7).ToList();
        UpdatedAt = now;
        return Result.Success();
    }
}
