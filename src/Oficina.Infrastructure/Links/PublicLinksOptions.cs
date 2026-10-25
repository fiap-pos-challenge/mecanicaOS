using System.ComponentModel.DataAnnotations;

namespace Oficina.Infrastructure.Links;

public sealed class PublicLinksOptions
{
    public const string SectionName = "PublicLinks";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:4200";
}
