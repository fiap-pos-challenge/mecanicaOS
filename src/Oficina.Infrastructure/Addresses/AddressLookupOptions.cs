using System.ComponentModel.DataAnnotations;

namespace Oficina.Infrastructure.Addresses;

public sealed class AddressLookupOptions
{
    public const string SectionName = "AddressLookup";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://viacep.com.br/";

    [Range(1, 30)]
    public int TimeoutSeconds { get; set; } = 5;
}
