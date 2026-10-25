using System.Text.Json;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports;

public static class ReportDefinitions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Error? Check(ReportDefinitionDto? definition) =>
        !ReportEngine.IsReadable(definition) ? ReportErrors.InvalidDefinition
        : ReportEngine.Problem(definition!) is { } problem ? ReportErrors.Incompatible(problem)
        : null;

    public static SavedReportDto ToDto(this SavedReport report) => new(
        report.Id,
        report.Name,
        JsonSerializer.Deserialize<ReportDefinitionDto>(report.DefinitionJson, Json)!,
        report.ScheduleJson is null ? null : JsonSerializer.Deserialize<ReportScheduleDto>(report.ScheduleJson, Json),
        report.UpdatedAt);

    public static async Task<Error?> NameProblemAsync(ISavedReportRepository reports, string name, Guid? id, CancellationToken cancellationToken)
    {
        if (name.Length is < 3 or > SavedReport.NameMaxLength)
        {
            return ReportErrors.InvalidName;
        }

        return await reports.NameExistsAsync(name, id, cancellationToken) ? ReportErrors.DuplicateName : null;
    }
}
