using System.Globalization;
using Oficina.Application.Analytics;
using Oficina.Application.Common.Numbers;

namespace Oficina.Application.Reports;

public static class ReportEngine
{
    public const int MaxCustomDays = 366;
    public static readonly string[] Measures = ["revenue", "orderCount", "partsOut", "averageTicket", "margin", "commission"];
    public static readonly string[] Groupings = ["month", "week", "mechanic", "service", "category", "paymentMethod", "customer"];
    public static readonly string[] Presets = ["thisMonth", "lastMonth", "lastQuarter", "yearToDate", "custom"];
    public static readonly string[] Charts = ["column", "line", "donut", "table"];
    public static readonly IReadOnlyDictionary<string, string> MeasureNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["revenue"] = "Faturamento", ["orderCount"] = "Quantidade de OS", ["partsOut"] = "Saída de peças", ["averageTicket"] = "Ticket médio", ["margin"] = "Margem", ["commission"] = "Comissão",
    };

    public static readonly IReadOnlyDictionary<string, string> GroupingNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["month"] = "Mês", ["week"] = "Semana", ["mechanic"] = "Mecânico", ["service"] = "Serviço", ["category"] = "Categoria", ["paymentMethod"] = "Forma de pagamento", ["customer"] = "Cliente",
    };

    public static readonly IReadOnlyDictionary<string, string> PresetNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["thisMonth"] = "Este mês", ["lastMonth"] = "Mês passado", ["lastQuarter"] = "Último trimestre", ["yearToDate"] = "Ano até hoje", ["custom"] = "Período personalizado",
    };

    private static readonly string[] TimeGroupings = ["month", "week"];
    private static readonly string[] Additive = ["revenue", "orderCount", "partsOut", "commission"];
    private static readonly string[] MonthNames = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];
    private static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["revenue"] = Groupings,
        ["orderCount"] = Groupings,
        ["averageTicket"] = Groupings,
        ["partsOut"] = ["month", "week", "mechanic", "service", "category", "customer"],
        ["margin"] = ["month", "week", "mechanic", "service", "category", "customer"],
        ["commission"] = ["month", "week", "mechanic", "service"],
    };

    private sealed record Contribution(string Key, string Sort, Guid OrderId, decimal Revenue, decimal Quantity, decimal Cost, decimal Commission);

    public static IReadOnlyList<string> AllowedGroupings(string measure) => Allowed.TryGetValue(measure, out var groups) ? groups : [];

    public static bool IsReadable(ReportDefinitionDto? definition) =>
        definition is { Period: not null } && Measures.Contains(definition.Measure) && Groupings.Contains(definition.GroupBy) && Charts.Contains(definition.Chart) && Presets.Contains(definition.Period.Preset);

    public static string? Problem(ReportDefinitionDto definition)
    {
        var overTime = TimeGroupings.Contains(definition.GroupBy);
        if (!AllowedGroupings(definition.Measure).Contains(definition.GroupBy))
        {
            return "Este indicador não pode ser agrupado assim.";
        }

        if (definition.Chart == "line" && !overTime)
        {
            return "O gráfico de linha precisa de mês ou semana.";
        }

        if (definition.Chart == "donut" && overTime)
        {
            return "O gráfico de rosca precisa de partes, não de tempo.";
        }

        if (definition.Chart == "donut" && !Additive.Contains(definition.Measure))
        {
            return "O gráfico de rosca só vale para valores que somam.";
        }

        if (definition.Period.Preset == "custom")
        {
            if (!TryDate(definition.Period.From, out var from) || !TryDate(definition.Period.To, out var to))
            {
                return "Informe o início e o fim do período.";
            }

            if (to < from)
            {
                return "O início precisa vir antes do fim.";
            }

            if (to.DayNumber - from.DayNumber + 1 > MaxCustomDays)
            {
                return "O período pode ter até 366 dias.";
            }
        }

        return null;
    }

    public static (DateOnly From, DateOnly To) Range(ReportPeriodDto period, DateOnly today)
    {
        var month = new DateOnly(today.Year, today.Month, 1);
        return period.Preset switch
        {
            "thisMonth" => (month, today),
            "lastMonth" => (month.AddMonths(-1), month.AddDays(-1)),
            "lastQuarter" => (month.AddMonths(-3), month.AddDays(-1)),
            "yearToDate" => (new DateOnly(today.Year, 1, 1), today),
            _ => (TryDate(period.From, out var from) ? from : today, TryDate(period.To, out var to) ? to : today),
        };
    }

    public static ReportResultDto Run(ReportDefinitionDto definition, IReadOnlyList<SaleFact> sales, DateOnly from, DateOnly to, TimeSpan offset)
    {
        var unit = definition.Measure switch { "revenue" or "averageTicket" or "commission" => "money", "margin" => "percent", _ => "count" };
        var contributions = sales.SelectMany(sale => Contribute(definition.GroupBy, sale, offset)).ToList();
        var groups = contributions.GroupBy(item => item.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var keys = definition.GroupBy switch
        {
            "month" => Months(from, to),
            "week" => Weeks(from, to),
            _ => groups.Values.OrderByDescending(group => Measure(definition.Measure, group)).ThenBy(group => group[0].Key, StringComparer.Ordinal).Select(group => (Label: group[0].Key, Sort: group[0].Sort)).ToList(),
        };
        var rows = keys.Select(key => new ReportRowDto(key.Label, Measure(definition.Measure, groups.GetValueOrDefault(key.Label) ?? []))).ToList();
        decimal? total = Additive.Contains(definition.Measure) ? rows.Sum(row => row.Value) : null;
        return new ReportResultDto(rows, unit, total, Iso(from), Iso(to));
    }

    public static string Explain(ReportDefinitionDto definition) =>
        $"{MeasureNames[definition.Measure]} por {GroupingNames[definition.GroupBy].ToLowerInvariant()}, {PresetNames[definition.Period.Preset].ToLowerInvariant()}.";

    public static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Format(string unit, decimal value) => unit switch
    {
        "money" => (value / 100).ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
        "percent" => $"{value.ToString("0.#", CultureInfo.GetCultureInfo("pt-BR"))}%",
        _ => value.ToString("0.###", CultureInfo.GetCultureInfo("pt-BR")),
    };

    private static bool TryDate(string? value, out DateOnly date) => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static decimal Measure(string measure, IReadOnlyCollection<Contribution> items)
    {
        var revenue = items.Sum(item => item.Revenue);
        var orders = items.Select(item => item.OrderId).Distinct().Count();
        return measure switch
        {
            "revenue" => Cents(revenue),
            "orderCount" => orders,
            "partsOut" => items.Sum(item => item.Quantity),
            "averageTicket" => orders == 0 ? 0 : Cents(revenue / orders),
            "margin" => revenue == 0 ? 0 : Rounding.HalfUp((revenue - items.Sum(item => item.Cost)) * 100 / revenue, 1),
            _ => Cents(items.Sum(item => item.Commission)),
        };
    }

    private static decimal Cents(decimal value) => Rounding.HalfUp(value * 100, 0);

    private static IEnumerable<Contribution> Contribute(string groupBy, SaleFact sale, TimeSpan offset)
    {
        var quantity = sale.Parts.Sum(line => line.Quantity);
        var cost = sale.Parts.Sum(line => line.Cost);
        var commission = sale.Services.Sum(line => line.Commission);
        var local = DateOnly.FromDateTime(sale.DeliveredAt.ToOffset(offset).DateTime);
        switch (groupBy)
        {
            case "service":
                foreach (var line in sale.Services)
                {
                    yield return new Contribution(line.Name, line.Name, sale.OrderId, line.Total, 0, 0, line.Commission);
                }

                break;
            case "category":
                if (sale.Services.Count > 0)
                {
                    yield return new Contribution("Mão de obra", "Mão de obra", sale.OrderId, sale.Services.Sum(line => line.Total), 0, 0, commission);
                }

                foreach (var group in sale.Parts.GroupBy(line => line.Category, StringComparer.Ordinal))
                {
                    yield return new Contribution(group.Key, group.Key, sale.OrderId, group.Sum(line => line.Total), group.Sum(line => line.Quantity), group.Sum(line => line.Cost), 0);
                }

                break;
            default:
                var key = groupBy switch
                {
                    "month" => MonthLabel(local),
                    "week" => WeekLabel(Monday(local)),
                    "mechanic" => sale.MechanicName,
                    "paymentMethod" => sale.PaymentMethod,
                    _ => sale.CustomerName,
                };
                yield return new Contribution(key, key, sale.OrderId, sale.Total, quantity, cost, commission);
                break;
        }
    }

    private static List<(string Label, string Sort)> Months(DateOnly from, DateOnly to)
    {
        var labels = new List<(string, string)>();
        for (var cursor = new DateOnly(from.Year, from.Month, 1); cursor <= to; cursor = cursor.AddMonths(1))
        {
            labels.Add((MonthLabel(cursor), Iso(cursor)));
        }

        return labels;
    }

    private static List<(string Label, string Sort)> Weeks(DateOnly from, DateOnly to)
    {
        var labels = new List<(string, string)>();
        for (var cursor = Monday(from); cursor <= to; cursor = cursor.AddDays(7))
        {
            labels.Add((WeekLabel(cursor), Iso(cursor)));
        }

        return labels;
    }

    private static DateOnly Monday(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static string MonthLabel(DateOnly date) => $"{MonthNames[date.Month - 1]}/{(date.Year % 100).ToString("00", CultureInfo.InvariantCulture)}";

    private static string WeekLabel(DateOnly monday) => $"Sem. {monday.ToString("dd/MM", CultureInfo.InvariantCulture)}";
}
