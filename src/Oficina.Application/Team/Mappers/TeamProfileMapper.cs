using System.Globalization;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Team.Entities;

namespace Oficina.Application.Team.Mappers;

public static class TeamProfileMapper
{
    private const string TimeFormat = "HH:mm";

    public static TeamProfileDto ToDto(this TeamProfile profile) => new(
        profile.UserId,
        profile.Specialties,
        profile.CommissionPercent,
        profile.DiscountLimitPercent,
        profile.Schedule.Select(shift => new WorkShiftDto(
            shift.Day.ToString(),
            shift.Start.ToString(TimeFormat, CultureInfo.InvariantCulture),
            shift.End.ToString(TimeFormat, CultureInfo.InvariantCulture))).ToList(),
        profile.UpdatedAt);

    public static WorkShift? ToShift(WorkShiftDto dto) =>
        Enum.TryParse<DayOfWeek>(dto.Day, ignoreCase: false, out var day)
        && Enum.IsDefined(day)
        && TimeOnly.TryParseExact(dto.Start, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
        && TimeOnly.TryParseExact(dto.End, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
            ? new WorkShift(day, start, end)
            : null;
}
