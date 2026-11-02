using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Oficina.Application.Common.Behaviors;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.UnitTests.Application.Common.Behaviors;

public sealed class LoggingBehaviorTests
{
    private const string SensitiveValue = "52998224725";
    private readonly FakeLogger<LoggingBehavior<SensitiveRequest, Result>> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly LoggingBehavior<SensitiveRequest, Result> _behavior;

    public LoggingBehaviorTests() => _behavior = new LoggingBehavior<SensitiveRequest, Result>(_logger, _timeProvider);

    [Fact]
    public async Task Handle_Success_LogsRequestNameAndElapsedTimeWithoutPayload()
    {
        var result = await _behavior.Handle(new SensitiveRequest(SensitiveValue), _ =>
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(42));
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal("Request SensitiveRequest succeeded in 42 ms", record.Message);
        Assert.DoesNotContain(SensitiveValue, record.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_Failure_LogsErrorCodeWithoutPayload()
    {
        var result = await _behavior.Handle(new SensitiveRequest(SensitiveValue), _ => Task.FromResult<Result>(CommonErrors.DuplicateRecord), CancellationToken.None);

        Assert.True(result.IsFailure);
        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal("Request SensitiveRequest failed with common.duplicateRecord in 0 ms", record.Message);
        Assert.DoesNotContain(SensitiveValue, record.Message, StringComparison.Ordinal);
    }

    public sealed record SensitiveRequest(string Document);
}
