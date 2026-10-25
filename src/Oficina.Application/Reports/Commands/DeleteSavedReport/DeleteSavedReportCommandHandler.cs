using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports.Commands.DeleteSavedReport;

public sealed class DeleteSavedReportCommandHandler(ISavedReportRepository reports, IUnitOfWork unitOfWork) : IRequestHandler<DeleteSavedReportCommand, Result>
{
    public async Task<Result> Handle(DeleteSavedReportCommand command, CancellationToken cancellationToken)
    {
        if (await reports.GetByIdAsync(command.Id, cancellationToken) is not { } report)
        {
            return ReportErrors.NotFound;
        }

        reports.Remove(report);
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
