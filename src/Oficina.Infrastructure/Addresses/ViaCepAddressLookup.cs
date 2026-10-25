using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Addresses;
using Oficina.Application.Addresses.Dtos;

namespace Oficina.Infrastructure.Addresses;

public sealed partial class ViaCepAddressLookup(HttpClient client, ILogger<ViaCepAddressLookup> logger) : IAddressLookup
{
    public async Task<AddressLookupDto?> FindAsync(string zipCode, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetFromJsonAsync<ViaCepResponse>($"ws/{zipCode}/json/", cancellationToken);
            if (response is null || response.Erro is true || string.IsNullOrWhiteSpace(response.Localidade))
            {
                return null;
            }

            return new AddressLookupDto(
                zipCode,
                response.Logradouro ?? string.Empty,
                null,
                null,
                response.Bairro ?? string.Empty,
                response.Localidade,
                (response.Uf ?? string.Empty).ToUpperInvariant());
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            LogLookupFailed(logger, exception.GetType().Name);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Zip code lookup failed with {ErrorType}")]
    private static partial void LogLookupFailed(ILogger logger, string errorType);

    private sealed record ViaCepResponse(string? Logradouro, string? Bairro, string? Localidade, string? Uf, bool? Erro);
}
