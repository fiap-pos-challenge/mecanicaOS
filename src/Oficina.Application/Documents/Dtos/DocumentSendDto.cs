namespace Oficina.Application.Documents.Dtos;

public sealed record DocumentSendDto(string Status)
{
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Queued = "Queued";
}
