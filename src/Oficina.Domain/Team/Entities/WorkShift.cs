namespace Oficina.Domain.Team.Entities;

public sealed record WorkShift(DayOfWeek Day, TimeOnly Start, TimeOnly End);
