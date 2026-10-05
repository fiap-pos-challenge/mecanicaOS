using Oficina.Domain.Common.ValueObjects;

namespace Oficina.UnitTests.Domain.Common.ValueObjects;

public sealed class ValueObjectTests
{
    private sealed class Point(int x, int y) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return x;
            yield return y;
        }
    }

    [Fact]
    public void Equals_SameComponents_AreEqual()
    {
        Assert.Equal(new Point(1, 2), new Point(1, 2));
        Assert.True(new Point(1, 2) == new Point(1, 2));
        Assert.Equal(new Point(1, 2).GetHashCode(), new Point(1, 2).GetHashCode());
    }

    [Fact]
    public void Equals_DifferentComponents_AreNotEqual()
    {
        Assert.NotEqual(new Point(1, 2), new Point(2, 1));
        Assert.True(new Point(1, 2) != new Point(2, 1));
    }

    [Fact]
    public void Equals_Null_IsFalse()
    {
        Assert.False(new Point(1, 2).Equals(null));
    }
}
