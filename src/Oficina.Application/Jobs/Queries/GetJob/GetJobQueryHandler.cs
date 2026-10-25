using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Jobs.Dtos;
using Oficina.Application.Jobs.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Jobs.Queries.GetJob;

public sealed class GetJobQueryHandler(IBackgroundJobRepository jobs, IUserRepository users, ICurrentUser currentUser, JobPresenter presenter) : IRequestHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery query, CancellationToken cancellationToken)
    {
        if (await jobs.GetByIdAsync(query.Id, cancellationToken) is not { } job)
        {
            return JobErrors.NotFound;
        }

        if (job.Kind == BackgroundJob.CustomerDataExport && !await CanExportAsync(cancellationToken))
        {
            return JobErrors.NotFound;
        }

        return await presenter.DtoAsync(job, cancellationToken);
    }

    private async Task<bool> CanExportAsync(CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId && await users.GetByIdAsync(userId, cancellationToken) is { Role: Role.Admin or Role.Manager };
}
