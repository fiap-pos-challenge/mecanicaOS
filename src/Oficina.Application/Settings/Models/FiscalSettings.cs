namespace Oficina.Application.Settings.Models;

public sealed record FiscalSettings(IReadOnlyList<FiscalIntegrationSettings> Integrations);
