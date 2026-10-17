namespace Oficina.Application.Common.Numbers;

public static class Rounding
{
    public static decimal HalfUp(decimal value, int decimals)
    {
        var factor = (decimal)Math.Pow(10, decimals);
        var scaled = value * factor;
        var truncated = decimal.Truncate(scaled);
        if (Math.Abs(scaled - truncated) >= 0.5m)
        {
            truncated += Math.Sign(scaled);
        }

        return truncated / factor;
    }
}
