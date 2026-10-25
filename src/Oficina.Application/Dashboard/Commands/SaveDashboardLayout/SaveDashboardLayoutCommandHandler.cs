using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Dashboard.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Dashboard;

namespace Oficina.Application.Dashboard.Commands.SaveDashboardLayout;

public sealed class SaveDashboardLayoutCommandHandler(DashboardLayouts layouts, IUnitOfWork unitOfWork) : IRequestHandler<SaveDashboardLayoutCommand, Result<DashboardLayoutDto>>
{
    public async Task<Result<DashboardLayoutDto>> Handle(SaveDashboardLayoutCommand command, CancellationToken cancellationToken)
    {
        if (command.Cards is not { } cards || cards.Count > DashboardLayouts.Cards.Count || cards.Any(card => !DashboardLayouts.Cards.Contains(card.Id))
            || cards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != cards.Count)
        {
            return DashboardErrors.InvalidLayout;
        }

        var saved = cards.Select(card => new DashboardCardDto(card.Id, card.Visible)).ToList();
        await layouts.SaveAsync(saved, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : new DashboardLayoutDto(saved);
    }
}
