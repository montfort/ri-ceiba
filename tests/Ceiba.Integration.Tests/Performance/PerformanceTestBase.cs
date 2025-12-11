using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Ceiba.Integration.Tests.Performance;

/// <summary>
/// Base class for performance tests.
/// T121-T125: NFR validation tests for performance requirements.
/// </summary>
[Trait("Category", "Performance")]
public abstract class PerformanceTestBase : IDisposable
{
    protected readonly ITestOutputHelper Output;
    protected readonly Stopwatch Stopwatch;

    protected PerformanceTestBase(ITestOutputHelper output)
    {
        Output = output;
        Stopwatch = new Stopwatch();
    }

    /// <summary>
    /// Measures execution time and asserts it's within the threshold.
    /// </summary>
    protected async Task<T> MeasureAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        TimeSpan maxDuration)
    {
        Stopwatch.Restart();

        var result = await operation();

        Stopwatch.Stop();

        var elapsed = Stopwatch.Elapsed;
        Output.WriteLine($"{operationName}: {elapsed.TotalMilliseconds:F2}ms (max: {maxDuration.TotalMilliseconds}ms)");

        Assert.True(
            elapsed <= maxDuration,
            $"{operationName} took {elapsed.TotalMilliseconds:F2}ms, exceeding the {maxDuration.TotalMilliseconds}ms threshold");

        return result;
    }

    /// <summary>
    /// Measures execution time without return value.
    /// </summary>
    protected async Task MeasureAsync(
        string operationName,
        Func<Task> operation,
        TimeSpan maxDuration)
    {
        await MeasureAsync(operationName, async () =>
        {
            await operation();
            return true;
        }, maxDuration);
    }

    /// <summary>
    /// Runs multiple iterations and reports statistics.
    /// </summary>
    protected async Task<PerformanceStats> RunIterationsAsync(
        string operationName,
        Func<Task> operation,
        int iterations,
        TimeSpan? warmupDuration = null)
    {
        var times = new List<double>();

        // Warmup
        if (warmupDuration.HasValue)
        {
            Output.WriteLine($"Warming up for {warmupDuration.Value.TotalSeconds}s...");
            var warmupEnd = DateTime.UtcNow + warmupDuration.Value;
            while (DateTime.UtcNow < warmupEnd)
            {
                await operation();
            }
        }

        // Actual measurements
        Output.WriteLine($"Running {iterations} iterations...");

        for (var i = 0; i < iterations; i++)
        {
            Stopwatch.Restart();
            await operation();
            Stopwatch.Stop();
            times.Add(Stopwatch.Elapsed.TotalMilliseconds);
        }

        var stats = new PerformanceStats
        {
            OperationName = operationName,
            Iterations = iterations,
            MinMs = times.Min(),
            MaxMs = times.Max(),
            AvgMs = times.Average(),
            MedianMs = GetMedian(times),
            P95Ms = GetPercentile(times, 95),
            P99Ms = GetPercentile(times, 99),
            StdDevMs = GetStandardDeviation(times)
        };

        Output.WriteLine(stats.ToString());

        return stats;
    }

    /// <summary>
    /// Simulates concurrent users executing operations.
    /// </summary>
    protected async Task<ConcurrencyStats> RunConcurrentAsync(
        string operationName,
        Func<int, Task> operation,
        int concurrentUsers,
        TimeSpan duration)
    {
        Output.WriteLine($"Running {concurrentUsers} concurrent users for {duration.TotalSeconds}s...");

        var cts = new CancellationTokenSource(duration);
        var completedOperations = 0;
        var errors = 0;
        var responseTimes = new List<double>();
        var lockObj = new object();

        var tasks = Enumerable.Range(0, concurrentUsers).Select(async userId =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await operation(userId);
                    sw.Stop();

                    lock (lockObj)
                    {
                        completedOperations++;
                        responseTimes.Add(sw.Elapsed.TotalMilliseconds);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    lock (lockObj)
                    {
                        errors++;
                    }
                }
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        var stats = new ConcurrencyStats
        {
            OperationName = operationName,
            ConcurrentUsers = concurrentUsers,
            DurationSeconds = duration.TotalSeconds,
            TotalOperations = completedOperations,
            Errors = errors,
            OperationsPerSecond = completedOperations / duration.TotalSeconds,
            AvgResponseMs = responseTimes.Count > 0 ? responseTimes.Average() : 0,
            P95ResponseMs = responseTimes.Count > 0 ? GetPercentile(responseTimes, 95) : 0,
            ErrorRate = completedOperations > 0 ? (double)errors / (completedOperations + errors) * 100 : 0
        };

        Output.WriteLine(stats.ToString());

        return stats;
    }

    private static double GetMedian(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    private static double GetPercentile(List<double> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    private static double GetStandardDeviation(List<double> values)
    {
        var avg = values.Average();
        var sumSquares = values.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumSquares / values.Count);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Performance statistics for a single operation type.
/// </summary>
public class PerformanceStats
{
    public required string OperationName { get; init; }
    public int Iterations { get; init; }
    public double MinMs { get; init; }
    public double MaxMs { get; init; }
    public double AvgMs { get; init; }
    public double MedianMs { get; init; }
    public double P95Ms { get; init; }
    public double P99Ms { get; init; }
    public double StdDevMs { get; init; }

    public override string ToString() =>
        $"""
        Performance Stats: {OperationName}
        ----------------------------------------
        Iterations: {Iterations}
        Min:        {MinMs:F2}ms
        Max:        {MaxMs:F2}ms
        Avg:        {AvgMs:F2}ms
        Median:     {MedianMs:F2}ms
        P95:        {P95Ms:F2}ms
        P99:        {P99Ms:F2}ms
        StdDev:     {StdDevMs:F2}ms
        ----------------------------------------
        """;
}

/// <summary>
/// Concurrency statistics for load testing.
/// </summary>
public class ConcurrencyStats
{
    public required string OperationName { get; init; }
    public int ConcurrentUsers { get; init; }
    public double DurationSeconds { get; init; }
    public int TotalOperations { get; init; }
    public int Errors { get; init; }
    public double OperationsPerSecond { get; init; }
    public double AvgResponseMs { get; init; }
    public double P95ResponseMs { get; init; }
    public double ErrorRate { get; init; }

    public override string ToString() =>
        $"""
        Concurrency Stats: {OperationName}
        ----------------------------------------
        Concurrent Users:    {ConcurrentUsers}
        Duration:            {DurationSeconds:F1}s
        Total Operations:    {TotalOperations}
        Errors:              {Errors}
        Ops/Second:          {OperationsPerSecond:F2}
        Avg Response:        {AvgResponseMs:F2}ms
        P95 Response:        {P95ResponseMs:F2}ms
        Error Rate:          {ErrorRate:F2}%
        ----------------------------------------
        """;
}
