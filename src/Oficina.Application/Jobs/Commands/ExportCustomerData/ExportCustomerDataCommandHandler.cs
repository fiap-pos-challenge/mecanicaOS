using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Jobs.Dtos;
using Oficina.Application.Jobs.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.Application.Jobs.Commands.ExportCustomerData;

public sealed class ExportCustomerDataCommandHandler(
    IBackgroundJobRepository jobs,
    ICustomerRepository customers,
    JobPresenter presenter,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ExportCustomerDataCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(ExportCustomerDataCommand command, CancellationToken cancellationToken)
    {
        if (await customers.GetByIdAsync(command.CustomerId, cancellationToken) is null)
        {
            return CustomerErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        var job = BackgroundJob.Queue(BackgroundJob.CustomerDataExport, currentUser.UserId ?? Guid.Empty, command.CustomerId, now);
        job.Complete(JobMessages.ExportFileName(command.CustomerId), null, now);
        jobs.Add(job);
        var committed = await unitOfWork.CommitAsync(cancellationToken);
        return committed.Error is { } error ? error : await presenter.DtoAsync(job, cancellationToken);
    }
}
