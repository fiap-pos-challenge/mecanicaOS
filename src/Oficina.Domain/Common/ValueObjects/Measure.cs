namespace Oficina.Domain.Common.ValueObjects;

public static class Measure
{
    private const decimal Half = 0.5m;

    public static decimal Round(decimal value, int decimals)
    {
        var factor = Factor(decimals);
        var scaled = value * factor;
        var truncated = decimal.Truncate(scaled);
        if (Math.Abs(scaled - truncated) >= Half)
        {
            truncated += Math.Sign(scaled);
        }

        return truncated / factor;
    }

    public static decimal Percent(decimal part, decimal whole, int decimals) =>
        whole == 0 ? 0 : Round(part * 100 / whole, decimals);

    private static decimal Factor(int decimals)
    {
        var factor = 1m;
        for (var index = 0; index < decimals; index++)
        {
            factor *= 10;
        }

        return factor;
    }
}
