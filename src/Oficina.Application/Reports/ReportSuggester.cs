using System.Text.RegularExpressions;
using Oficina.Application.Reports.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Reports;

namespace Oficina.Application.Reports;

public static partial class ReportSuggester
{
    private static readonly string[] MonthNames = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];

    public static Result<ReportSuggestionDto> Suggest(string? question, DateOnly today)
    {
        var text = (question ?? string.Empty).Trim();
        if (text.Length is < 8 or > 300)
        {
            return ReportErrors.InvalidQuestion;
        }

        var lower = text.ToLowerInvariant();
        string? measure = lower switch
        {
            _ when lower.Contains("ticket", StringComparison.Ordinal) => "averageTicket",
            _ when lower.Contains("margem", StringComparison.Ordinal) => "margin",
            _ when lower.Contains("comiss", StringComparison.Ordinal) => "commission",
            _ when lower.Contains("peça", StringComparison.Ordinal) || lower.Contains("peca", StringComparison.Ordinal) => "partsOut",
            _ when QuantityPattern().IsMatch(lower) => "orderCount",
            _ when RevenuePattern().IsMatch(lower) => "revenue",
            _ => null,
        };
        if (measure is null)
        {
            return ReportErrors.NotUnderstood;
        }

        string wanted = lower switch
        {
            _ when MechanicPattern().IsMatch(lower) => "mechanic",
            _ when lower.Contains("pagamento", StringComparison.Ordinal) => "paymentMethod",
            _ when lower.Contains("categoria", StringComparison.Ordinal) => "category",
            _ when lower.Contains("serviço", StringComparison.Ordinal) || lower.Contains("servico", StringComparison.Ordinal) => "service",
            _ when lower.Contains("cliente", StringComparison.Ordinal) => "customer",
            _ when lower.Contains("semana", StringComparison.Ordinal) => "week",
            _ => "month",
        };
        var groupBy = ReportEngine.AllowedGroupings(measure).Contains(wanted) ? wanted : "month";
        var monthIndex = Array.FindIndex(MonthNames, name => lower.Contains(name, StringComparison.Ordinal));
        var period = monthIndex >= 0
            ? new ReportPeriodDto("custom", ReportEngine.Iso(new DateOnly(today.Year, monthIndex + 1, 1)), ReportEngine.Iso(new DateOnly(today.Year, monthIndex + 1, 1).AddMonths(1).AddDays(-1)))
            : lower.Contains("mês passado", StringComparison.Ordinal) || lower.Contains("mes passado", StringComparison.Ordinal) ? new ReportPeriodDto("lastMonth", null, null)
            : lower.Contains("trimestre", StringComparison.Ordinal) ? new ReportPeriodDto("lastQuarter", null, null)
            : lower.Contains("este mês", StringComparison.Ordinal) || lower.Contains("esse mês", StringComparison.Ordinal) || lower.Contains("este mes", StringComparison.Ordinal) ? new ReportPeriodDto("thisMonth", null, null)
            : new ReportPeriodDto("yearToDate", null, null);
        var overTime = groupBy is "month" or "week";
        var additive = measure is not ("averageTicket" or "margin");
        var chart = overTime ? "line" : additive && SharePattern().IsMatch(lower) ? "donut" : "column";
        var definition = new ReportDefinitionDto(measure, groupBy, period, chart);
        return new ReportSuggestionDto(definition, ReportEngine.Explain(definition));
    }

    [GeneratedRegex("quant|quantas|número de os|numero de os")]
    private static partial Regex QuantityPattern();

    [GeneratedRegex("fatur|vend|receita")]
    private static partial Regex RevenuePattern();

    [GeneratedRegex("mecânic|mecanic")]
    private static partial Regex MechanicPattern();

    [GeneratedRegex("parte|divid|particip")]
    private static partial Regex SharePattern();
}
