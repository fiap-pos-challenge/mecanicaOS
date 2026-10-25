using System.Text.Json;
using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Jobs.Dtos;
using Oficina.Application.Jobs.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Jobs.Commands.ImportCustomers;

public sealed class ImportCustomersCommandHandler(
    IBackgroundJobRepository jobs,
    CustomerCsvImporter importer,
    JobPresenter presenter,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<ImportCustomersCommand, Result<JobDto>>
{
    public const int MaxImportBytes = 2 * 1024 * 1024;

    public async Task<Result<JobDto>> Handle(ImportCustomersCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Csv) || command.FileName is null || !command.FileName.Trim().EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return JobErrors.InvalidFile;
        }

        if (command.Csv.Length > MaxImportBytes)
        {
            return JobErrors.FileTooLarge;
        }

        var now = timeProvider.GetUtcNow();
        var report = await importer.ImportAsync(command.Csv, cancellationToken);
        var job = BackgroundJob.Queue(BackgroundJob.CustomerImport, currentUser.UserId ?? Guid.Empty, null, now);
        job.Complete(null, JsonSerializer.Serialize(report, JobPresenter.Compact), now);
        jobs.Add(job);
        var committed = await unitOfWork.CommitAsync(cancellationToken);
        return committed.Error is { } error ? error : await presenter.DtoAsync(job, cancellationToken);
    }
}
