namespace Oficina.Application.Jobs.Models;

public sealed class BackgroundJob
{
    public const string CustomerDataExport = "CustomerDataExport";
    public const string CustomerImport = "CustomerImport";
    public const string Queued = "Queued";
    public const string Completed = "Completed";
    public const string Failed = "Failed";

    private BackgroundJob(Guid id, string kind, Guid requestedBy, Guid? customerId, DateTimeOffset createdAt)
    {
        Id = id;
        Kind = kind;
        RequestedBy = requestedBy;
        CustomerId = customerId;
        CreatedAt = createdAt;
        Status = Queued;
    }

    public Guid Id { get; private init; }

    public string Kind { get; private init; }

    public Guid RequestedBy { get; private init; }

    public Guid? CustomerId { get; private init; }

    public string Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FileName { get; private set; }

    public string? ResultJson { get; private set; }

    public static BackgroundJob Queue(string kind, Guid requestedBy, Guid? customerId, DateTimeOffset now) => new(Guid.CreateVersion7(), kind, requestedBy, customerId, now);

    public void Complete(string? fileName, string? resultJson, DateTimeOffset now)
    {
        Status = Completed;
        FileName = fileName;
        ResultJson = resultJson;
        CompletedAt = now;
    }

    public void Fail(DateTimeOffset now)
    {
        Status = Failed;
        CompletedAt = now;
    }
}
