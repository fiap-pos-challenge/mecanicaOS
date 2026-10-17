using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Numbers;
using Oficina.Application.Team.Dtos;
using Oficina.Application.Team.Mappers;
using Oficina.Application.Team.Queries;
using Oficina.Domain.Team.Entities;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Team;

public sealed class TeamQueries(OficinaDbContext context) : ITeamQueries
{
    public async Task<IReadOnlyList<TeamProfileDto>> ListProfilesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var stored = await context.TeamProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);
        return userIds.Select(id => (stored.GetValueOrDefault(id) ?? TeamProfile.Empty(id)).ToDto()).ToList();
    }

    public async Task<IReadOnlyList<ProductivityDto>> ProductivityAsync(IReadOnlyCollection<Guid> userIds, int periodDays, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var from = now.AddDays(-periodDays);
        var weeks = (int)Rounding.HalfUp(periodDays / 7m, 0);
        var schedules = await context.TeamProfiles.AsNoTracking()
            .Where(profile => userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, profile => profile.Schedule, cancellationToken);
        var entries = await context.TimeEntries.AsNoTracking()
            .Where(entry => userIds.Contains(entry.MechanicId) && entry.StartedAt >= from && entry.EndedAt != null)
            .Select(entry => new { entry.MechanicId, entry.ServiceOrderId, entry.StartedAt, Minutes = entry.Minutes })
            .ToListAsync(cancellationToken);
        var orderIds = entries.Select(entry => entry.ServiceOrderId).Distinct().ToList();
        var orders = await context.ServiceOrders.AsNoTracking()
            .Where(order => orderIds.Contains(order.Id))
            .Select(order => new { order.Id, order.TotalAmount, order.Reopened })
            .ToDictionaryAsync(order => order.Id, cancellationToken);

        return userIds.Select(userId =>
        {
            var mine = entries.Where(entry => entry.MechanicId == userId).ToList();
            var mineOrders = mine.Select(entry => entry.ServiceOrderId).Distinct().Where(orders.ContainsKey).Select(id => orders[id]).ToList();
            var hours = Rounding.HalfUp(mine.Sum(entry => entry.Minutes) / 60m, 1);
            var weeklyCapacity = schedules.GetValueOrDefault(userId)?.Sum(shift => (decimal)(shift.End - shift.Start).TotalHours) ?? 44m;
            var capacity = weeklyCapacity * periodDays / 7m;
            var weekly = Enumerable.Range(0, Math.Max(weeks, 1))
                .Select(week => mine.Where(entry => (int)((entry.StartedAt - from).TotalDays / 7) == week).Select(entry => entry.ServiceOrderId).Distinct().Count())
                .ToList();
            return new ProductivityDto(
                userId,
                mineOrders.Count,
                hours,
                capacity == 0 ? 0 : Math.Min(100, Rounding.HalfUp(hours / capacity * 100, 1)),
                mineOrders.Sum(order => order.TotalAmount.Amount),
                mineOrders.Count == 0 ? 0 : Rounding.HalfUp(mineOrders.Count(order => order.Reopened) * 100m / mineOrders.Count, 1),
                weekly);
        }).ToList();
    }
}
