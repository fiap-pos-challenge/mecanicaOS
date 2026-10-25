using System.ComponentModel.DataAnnotations;

namespace Oficina.Infrastructure.Notifications;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? SmtpHost { get; set; }

    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 25;

    public string? PickupDirectory { get; set; }

    [Required]
    [EmailAddress]
    public string From { get; set; } = "nao-responda@oficina.local";

    [Required]
    [Url]
    public string InvitationUrl { get; set; } = "http://localhost:4200/invite";
}
