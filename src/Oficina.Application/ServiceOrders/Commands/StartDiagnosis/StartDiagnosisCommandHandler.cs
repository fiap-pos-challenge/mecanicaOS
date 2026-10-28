using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.StartDiagnosis;

public sealed class StartDiagnosisCommandHandler(ServiceOrderChanges changes, ICurrentUser currentUser, TimeProvider timeProvider)
    : IRequestHandler<StartDiagnosisCommand, Result<ServiceOrderDetailDto>>
{
    public Task<Result<ServiceOrderDetailDto>> Handle(StartDiagnosisCommand command, CancellationToken cancellationToken) =>
        changes.ApplyAsync(
            command.ServiceOrderId,
            serviceOrder => serviceOrder.StartDiagnosis(currentUser.UserId, timeProvider.GetUtcNow()),
            cancellationToken);
}
