using Oficina.Domain.Privacy.Entities;
using Oficina.Domain.Privacy.Enums;

namespace Oficina.UnitTests.Domain.Privacy;

public sealed class SensitiveDataAccessTests
{
    [Fact]
    public void Record_KeepsWhoSawWhatWhenAndWhy()
    {
        var userId = Guid.CreateVersion7();
        var customerId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        var access = SensitiveDataAccess.Record(userId, customerId, SensitiveField.Email, AccessReason.Edit, "abc", now);

        Assert.NotEqual(Guid.Empty, access.Id);
        Assert.Equal((userId, customerId, SensitiveField.Email, AccessReason.Edit, "abc", now), (access.UserId, access.CustomerId, access.Field, access.Reason, access.TraceId, access.OccurredAt));
    }

    [Fact]
    public void Record_TruncatesLongTraceIds()
    {
        var access = SensitiveDataAccess.Record(Guid.CreateVersion7(), Guid.CreateVersion7(), SensitiveField.Phone, AccessReason.View, new string('a', 100), DateTimeOffset.UnixEpoch);

        Assert.Equal(SensitiveDataAccess.TraceIdMaxLength, access.TraceId!.Length);
        Assert.Null(SensitiveDataAccess.Record(Guid.CreateVersion7(), Guid.CreateVersion7(), SensitiveField.Phone, AccessReason.View, null, DateTimeOffset.UnixEpoch).TraceId);
    }
}
