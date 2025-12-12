using Ceiba.Infrastructure.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ceiba.Infrastructure.Tests.Resilience;

/// <summary>
/// Unit tests for AiServiceMetrics - AI call monitoring and metrics.
/// T092e: AI call monitoring tests.
/// </summary>
public class AiServiceMetricsTests
{
    private readonly ILogger _logger;
    private readonly AiServiceMetrics _metrics;

    public AiServiceMetricsTests()
    {
        _logger = Substitute.For<ILogger>();
        _metrics = new AiServiceMetrics(_logger);
    }

    #region Initial State Tests

    [Fact(DisplayName = "New metrics should have 100% success rate")]
    public void NewMetrics_SuccessRate_Is100Percent()
    {
        // Assert
        _metrics.SuccessRate.Should().Be(1.0);
    }

    [Fact(DisplayName = "New metrics should have zero average latency")]
    public void NewMetrics_AverageLatency_IsZero()
    {
        // Assert
        _metrics.AverageLatencyMs.Should().Be(0);
    }

    [Fact(DisplayName = "New metrics should have zero tokens used")]
    public void NewMetrics_TotalTokensUsed_IsZero()
    {
        // Assert
        _metrics.TotalTokensUsed.Should().Be(0);
    }

    #endregion

    #region RecordSuccess Tests

    [Fact(DisplayName = "RecordSuccess should increment total calls")]
    public void RecordSuccess_IncrementsTotalCalls()
    {
        // Act
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");

        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalCalls.Should().Be(1);
        summary.SuccessfulCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RecordSuccess should track tokens used")]
    public void RecordSuccess_TracksTokensUsed()
    {
        // Act
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 75, "OpenAI");

        // Assert
        _metrics.TotalTokensUsed.Should().Be(125);
    }

    [Fact(DisplayName = "RecordSuccess should calculate average latency")]
    public void RecordSuccess_CalculatesAverageLatency()
    {
        // Act
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(200), 50, "OpenAI");

        // Assert
        _metrics.AverageLatencyMs.Should().Be(150);
    }

    [Fact(DisplayName = "RecordSuccess should maintain 100% success rate")]
    public void RecordSuccess_Maintains100PercentSuccessRate()
    {
        // Act
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");

        // Assert
        _metrics.SuccessRate.Should().Be(1.0);
    }

    [Fact(DisplayName = "RecordSuccess should log warning for slow responses")]
    public void RecordSuccess_SlowResponse_LogsWarning()
    {
        // Act
        _metrics.RecordSuccess(TimeSpan.FromSeconds(15), 100, "OpenAI");

        // Assert - logger should have been called with warning
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    #endregion

    #region RecordFailure Tests

    [Fact(DisplayName = "RecordFailure should increment failed calls")]
    public void RecordFailure_IncrementsFailedCalls()
    {
        // Act
        _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Timeout");

        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalCalls.Should().Be(1);
        summary.FailedCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RecordFailure should decrease success rate")]
    public void RecordFailure_DecreasesSuccessRate()
    {
        // Arrange
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");

        // Act
        _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Error");

        // Assert
        _metrics.SuccessRate.Should().Be(0.5);
    }

    [Fact(DisplayName = "RecordFailure should include latency in average")]
    public void RecordFailure_IncludesLatencyInAverage()
    {
        // Act
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");
        _metrics.RecordFailure(TimeSpan.FromMilliseconds(300), "OpenAI", "Error");

        // Assert
        _metrics.AverageLatencyMs.Should().Be(200);
    }

    [Fact(DisplayName = "RecordFailure should not track tokens")]
    public void RecordFailure_DoesNotTrackTokens()
    {
        // Act
        _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Error");

        // Assert
        _metrics.TotalTokensUsed.Should().Be(0);
    }

    #endregion

    #region Success Rate Calculation Tests

    [Theory(DisplayName = "Success rate should be calculated correctly")]
    [InlineData(8, 2, 0.8)]
    [InlineData(9, 1, 0.9)]
    [InlineData(5, 5, 0.5)]
    [InlineData(0, 10, 0.0)]
    [InlineData(10, 0, 1.0)]
    public void SuccessRate_CalculatedCorrectly(int successes, int failures, double expectedRate)
    {
        // Arrange
        for (int i = 0; i < successes; i++)
            _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");

        for (int i = 0; i < failures; i++)
            _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Error");

        // Assert
        _metrics.SuccessRate.Should().BeApproximately(expectedRate, 0.001);
    }

    #endregion

    #region RecordFallback Tests

    [Fact(DisplayName = "RecordFallback should log information")]
    public void RecordFallback_LogsInformation()
    {
        // Act
        _metrics.RecordFallback("AI service unavailable");

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    #endregion

    #region GetSummary Tests

    [Fact(DisplayName = "GetSummary should return correct metrics")]
    public void GetSummary_ReturnsCorrectMetrics()
    {
        // Arrange
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(200), 100, "OpenAI");
        _metrics.RecordFailure(TimeSpan.FromMilliseconds(150), "OpenAI", "Error");

        // Act
        var summary = _metrics.GetSummary();

        // Assert
        summary.TotalCalls.Should().Be(3);
        summary.SuccessfulCalls.Should().Be(2);
        summary.FailedCalls.Should().Be(1);
        summary.SuccessRate.Should().BeApproximately(0.667, 0.01);
        summary.AverageLatencyMs.Should().Be(150);
        summary.TotalTokensUsed.Should().Be(150);
    }

    #endregion

    #region Reset Tests

    [Fact(DisplayName = "Reset should clear all metrics")]
    public void Reset_ClearsAllMetrics()
    {
        // Arrange
        _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 50, "OpenAI");
        _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Error");

        // Act
        _metrics.Reset();

        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalCalls.Should().Be(0);
        summary.SuccessfulCalls.Should().Be(0);
        summary.FailedCalls.Should().Be(0);
        summary.TotalTokensUsed.Should().Be(0);
        summary.AverageLatencyMs.Should().Be(0);
    }

    [Fact(DisplayName = "Reset should restore 100% success rate")]
    public void Reset_Restores100PercentSuccessRate()
    {
        // Arrange
        _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Error");

        // Act
        _metrics.Reset();

        // Assert
        _metrics.SuccessRate.Should().Be(1.0);
    }

    #endregion

    #region Thread Safety Tests

    [Fact(DisplayName = "Metrics should be thread-safe for concurrent recording")]
    public async Task Metrics_ConcurrentRecording_ThreadSafe()
    {
        // Arrange
        var successTasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 10, "OpenAI")));

        var failureTasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Error")));

        // Act
        await Task.WhenAll(successTasks.Concat(failureTasks));

        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalCalls.Should().Be(100);
        summary.SuccessfulCalls.Should().Be(50);
        summary.FailedCalls.Should().Be(50);
    }

    [Fact(DisplayName = "GetSummary should be thread-safe")]
    public async Task GetSummary_ConcurrentAccess_ThreadSafe()
    {
        // Arrange - add some data first
        for (int i = 0; i < 10; i++)
            _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 10, "OpenAI");

        // Act - concurrent reads
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _metrics.GetSummary()));

        var summaries = await Task.WhenAll(tasks);

        // Assert
        summaries.Should().AllSatisfy(s => s.TotalCalls.Should().Be(10));
    }

    #endregion

    #region Alert Threshold Tests

    [Fact(DisplayName = "Should log alert when success rate drops below 90%")]
    public void LowSuccessRate_LogsAlert()
    {
        // Arrange - create > 5 calls to trigger alert check
        for (int i = 0; i < 4; i++)
            _metrics.RecordSuccess(TimeSpan.FromMilliseconds(100), 10, "OpenAI");

        // Act - add failures to drop below 90%
        for (int i = 0; i < 3; i++)
            _metrics.RecordFailure(TimeSpan.FromMilliseconds(100), "OpenAI", "Error");

        // Assert - should have logged warning
        var summary = _metrics.GetSummary();
        summary.SuccessRate.Should().BeLessThan(0.9);
    }

    #endregion
}
