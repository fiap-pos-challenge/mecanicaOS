using System.Text.Json;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Jobs.Entities;
using Oficina.Domain.Jobs.Errors;
using Oficina.Domain.Jobs.Repositories;

namespace Oficina.Application.Jobs;

public sealed class JobService(
    IBackgroundJobRepository jobs,
    ICustomerRepository customers,
    ICustomerInsightQueries insights,
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public const int MaxImportBytes = 2 * 1024 * 1024;
    public const int MaxSummaries = 500;
    private static readonly string[] Header = ["nome", "documento", "telefone", "email"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly JsonSerializerOptions Compact = new(JsonSerializerDefaults.Web);

    public async Task<Result<CustomerSummaryListDto>> SummariesAsync(IReadOnlyList<Guid>? customerIds, CancellationToken cancellationToken)
    {
        var ids = (customerIds ?? []).Distinct().ToList();
        if (ids.Count > MaxSummaries)
        {
            return JobErrors.TooManyCustomers;
        }

        var found = await insights.SummariesAsync(ids, timeProvider.GetUtcNow(), cancellationToken);
        return new CustomerSummaryListDto(ids.Select(id => found.FirstOrDefault(summary => summary.CustomerId == id) ?? new CustomerSummaryDto(id, 0, null, 0, 0, 0, 0, "NoReturn", false)).ToList());
    }

    public async Task<Result<ServiceHistoryDto>> HistoryAsync(Guid customerId, CancellationToken cancellationToken) =>
        await customers.GetByIdAsync(customerId, cancellationToken) is null
            ? CustomerErrors.NotFound
            : new ServiceHistoryDto(await insights.HistoryAsync(customerId, cancellationToken));

    public async Task<Result<JobDto>> ExportAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (await customers.GetByIdAsync(customerId, cancellationToken) is null)
        {
            return CustomerErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        var job = BackgroundJob.Queue(BackgroundJob.CustomerDataExport, currentUser.UserId ?? Guid.Empty, customerId, now);
        job.Complete($"dados-titular-{customerId.ToString("N")[..8]}.json", null, now);
        jobs.Add(job);
        var committed = await unitOfWork.CommitAsync(cancellationToken);
        return committed.Error is { } error ? error : await DtoAsync(job, cancellationToken);
    }

    public async Task<Result<JobDto>> ImportAsync(string? fileName, string? csv, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(csv) || fileName is null || !fileName.Trim().EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return JobErrors.InvalidFile;
        }

        if (csv.Length > MaxImportBytes)
        {
            return JobErrors.FileTooLarge;
        }

        var now = timeProvider.GetUtcNow();
        var report = await ImportCustomersAsync(csv, cancellationToken);
        var job = BackgroundJob.Queue(BackgroundJob.CustomerImport, currentUser.UserId ?? Guid.Empty, null, now);
        job.Complete(null, JsonSerializer.Serialize(report, Compact), now);
        jobs.Add(job);
        var committed = await unitOfWork.CommitAsync(cancellationToken);
        return committed.Error is { } error ? error : await DtoAsync(job, cancellationToken);
    }

    public async Task<Result<JobDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await jobs.GetByIdAsync(id, cancellationToken) is not { } job)
        {
            return JobErrors.NotFound;
        }

        if (job.Kind == BackgroundJob.CustomerDataExport && !await CanExportAsync(cancellationToken))
        {
            return JobErrors.NotFound;
        }

        return await DtoAsync(job, cancellationToken);
    }

    private async Task<bool> CanExportAsync(CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId && await users.GetByIdAsync(userId, cancellationToken) is { Role: Role.Admin or Role.Manager };

    private async Task<JobDto> DtoAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        JobResultDto? result = null;
        if (job.Status == "Completed" && job.Kind == BackgroundJob.CustomerDataExport && job.CustomerId is { } customerId)
        {
            var export = await insights.ExportAsync(customerId, timeProvider.GetUtcNow(), cancellationToken);
            result = new JobResultDto(job.FileName, export is null ? null : JsonSerializer.Serialize(export, Json), null, null, null);
        }
        else if (job.Status == "Completed" && job.ResultJson is { } json)
        {
            var report = JsonSerializer.Deserialize<ImportReport>(json, Compact)!;
            result = new JobResultDto(null, null, report.TotalLines, report.ValidLines, report.Errors);
        }

        return new JobDto(job.Id, job.Kind, job.Status, job.CreatedAt, job.CompletedAt, result);
    }

    private async Task<ImportReport> ImportCustomersAsync(string csv, CancellationToken cancellationToken)
    {
        var lines = csv.TrimStart('\uFEFF').Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Trim().Length > 0).ToList();
        var header = lines.Count > 0 ? lines[0] : string.Empty;
        var separator = header.Contains(';', StringComparison.Ordinal) ? ';' : ',';
        var columns = header.Split(separator).Select(column => column.Trim().ToLowerInvariant()).ToArray();
        var rows = lines.Skip(1).ToList();
        if (columns.Length < Header.Length || Header.Where((column, index) => columns[index] != column).Any())
        {
            return new ImportReport(rows.Count, 0, [new ImportErrorDto(1, "cabecalho", "O cabeçalho precisa ser nome;documento;telefone;email.")]);
        }

        var errors = new List<ImportErrorDto>();
        var documents = new HashSet<string>(StringComparer.Ordinal);
        var valid = 0;
        for (var index = 0; index < rows.Count; index++)
        {
            var line = index + 2;
            var cells = rows[index].Split(separator).Select(cell => cell.Trim()).ToArray();
            string Cell(int position) => position < cells.Length ? cells[position] : string.Empty;
            var lineErrors = new List<ImportErrorDto>();
            var name = Cell(0);
            if (name.Length < 2)
            {
                lineErrors.Add(new ImportErrorDto(line, "nome", "Informe o nome."));
            }

            var document = Document.Create(Cell(1));
            if (document.IsFailure)
            {
                lineErrors.Add(new ImportErrorDto(line, "documento", "CPF ou CNPJ inválido."));
            }

            var phone = Phone.Create(Cell(2));
            if (phone.IsFailure)
            {
                lineErrors.Add(new ImportErrorDto(line, "telefone", "Telefone inválido."));
            }

            Result<Email>? email = Cell(3).Length == 0 ? null : Email.Create(Cell(3));
            if (email is { IsFailure: true })
            {
                lineErrors.Add(new ImportErrorDto(line, "email", "E-mail inválido."));
            }

            if (lineErrors.Count == 0 && (!documents.Add(document.Value.Value) || await customers.GetByDocumentAsync(document.Value, cancellationToken) is not null))
            {
                lineErrors.Add(new ImportErrorDto(line, "documento", "Já existe cliente com este documento."));
            }

            if (lineErrors.Count == 0 && Customer.Create(name, document.Value, email?.Value, phone.Value, null) is { IsSuccess: true } customer)
            {
                customers.Add(customer.Value);
                valid++;
                continue;
            }

            errors.AddRange(lineErrors.Count > 0 ? lineErrors : [new ImportErrorDto(line, "nome", "Dados inválidos.")]);
        }

        return new ImportReport(rows.Count, valid, errors);
    }
}
