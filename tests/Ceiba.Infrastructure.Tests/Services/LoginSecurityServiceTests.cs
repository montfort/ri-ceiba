using Ceiba.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for LoginSecurityService (T113a-e: Login security features)
/// </summary>
public class LoginSecurityServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly LoginSecurityService _service;

    public LoginSecurityServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<LoginSecurityService>.Instance;
        _service = new LoginSecurityService(_cache, logger);
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    #region IsIpBlockedAsync Tests

    [Fact]
    public async Task IsIpBlockedAsync_NewIp_ShouldNotBeBlocked()
    {
        // Arrange
        var ipAddress = "192.168.1.1";

        // Act
        var isBlocked = await _service.IsIpBlockedAsync(ipAddress);

        // Assert
        isBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsIpBlockedAsync_AfterMaxAttempts_ShouldBeBlocked()
    {
        // Arrange
        var ipAddress = "192.168.1.2";
        var email = "test@example.com";

        // Act - Record 10 failed attempts
        for (int i = 0; i < 10; i++)
        {
            await _service.RecordFailedAttemptAsync(ipAddress, email);
        }

        var isBlocked = await _service.IsIpBlockedAsync(ipAddress);

        // Assert
        isBlocked.Should().BeTrue();
    }

    #endregion

    #region RecordFailedAttemptAsync Tests

    [Fact]
    public async Task RecordFailedAttemptAsync_ShouldIncrementCount()
    {
        // Arrange
        var ipAddress = "192.168.1.3";
        var email = "user@test.com";

        // Act
        await _service.RecordFailedAttemptAsync(ipAddress, email);
        await _service.RecordFailedAttemptAsync(ipAddress, email);
        await _service.RecordFailedAttemptAsync(ipAddress, email);

        var count = await _service.GetFailedAttemptCountAsync(ipAddress);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_DifferentEmails_ShouldTrackAll()
    {
        // Arrange
        var ipAddress = "192.168.1.4";

        // Act
        await _service.RecordFailedAttemptAsync(ipAddress, "user1@test.com");
        await _service.RecordFailedAttemptAsync(ipAddress, "user2@test.com");
        await _service.RecordFailedAttemptAsync(ipAddress, "user3@test.com");

        var count = await _service.GetFailedAttemptCountAsync(ipAddress);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_ExactlyMaxAttempts_ShouldLockout()
    {
        // Arrange
        var ipAddress = "192.168.1.5";
        var email = "test@example.com";

        // Act - Record exactly 10 failed attempts
        for (int i = 0; i < 10; i++)
        {
            await _service.RecordFailedAttemptAsync(ipAddress, email);
        }

        var isBlocked = await _service.IsIpBlockedAsync(ipAddress);
        var remainingSeconds = await _service.GetRemainingLockoutSecondsAsync(ipAddress);

        // Assert
        isBlocked.Should().BeTrue();
        remainingSeconds.Should().BeGreaterThan(0);
        remainingSeconds.Should().BeLessThanOrEqualTo(15 * 60); // Max 15 minutes
    }

    #endregion

    #region RecordSuccessfulLoginAsync Tests

    [Fact]
    public async Task RecordSuccessfulLoginAsync_ShouldClearFailedAttempts()
    {
        // Arrange
        var ipAddress = "192.168.1.6";
        var email = "user@test.com";

        // Act
        await _service.RecordFailedAttemptAsync(ipAddress, email);
        await _service.RecordFailedAttemptAsync(ipAddress, email);
        await _service.RecordFailedAttemptAsync(ipAddress, email);

        await _service.RecordSuccessfulLoginAsync(ipAddress, email);

        var count = await _service.GetFailedAttemptCountAsync(ipAddress);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region GetProgressiveDelayAsync Tests

    [Fact]
    public async Task GetProgressiveDelayAsync_NoFailedAttempts_ShouldReturnZero()
    {
        // Arrange
        var ipAddress = "192.168.1.7";

        // Act
        var delay = await _service.GetProgressiveDelayAsync(ipAddress);

        // Assert
        delay.Should().Be(0);
    }

    [Fact]
    public async Task GetProgressiveDelayAsync_LessThanThreshold_ShouldReturnZero()
    {
        // Arrange
        var ipAddress = "192.168.1.8";

        // Act
        await _service.RecordFailedAttemptAsync(ipAddress, "test@example.com");
        await _service.RecordFailedAttemptAsync(ipAddress, "test@example.com");

        var delay = await _service.GetProgressiveDelayAsync(ipAddress);

        // Assert
        delay.Should().Be(0);
    }

    [Fact]
    public async Task GetProgressiveDelayAsync_AfterThreshold_ShouldReturnDelay()
    {
        // Arrange
        var ipAddress = "192.168.1.9";

        // Act - 4 attempts (threshold is 3, so 4th attempt triggers delay)
        for (int i = 0; i < 4; i++)
        {
            await _service.RecordFailedAttemptAsync(ipAddress, "test@example.com");
        }

        var delay = await _service.GetProgressiveDelayAsync(ipAddress);

        // Assert - Should have a delay after exceeding threshold
        delay.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProgressiveDelayAsync_ShouldIncreaseProgressively()
    {
        // Arrange
        var ipAddress = "192.168.1.10";

        // Act - Record attempts and check progressive delays
        var delays = new List<int>();

        for (int i = 0; i < 8; i++)
        {
            await _service.RecordFailedAttemptAsync(ipAddress, "test@example.com");
            var delay = await _service.GetProgressiveDelayAsync(ipAddress);
            if (delay > 0)
            {
                delays.Add(delay);
            }
        }

        // Assert - Delays should increase
        delays.Should().HaveCountGreaterThan(1);
        delays.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetProgressiveDelayAsync_ShouldNotExceedMaxDelay()
    {
        // Arrange
        var ipAddress = "192.168.1.11";

        // Act - Many failed attempts
        for (int i = 0; i < 9; i++) // Just under lockout
        {
            await _service.RecordFailedAttemptAsync(ipAddress, "test@example.com");
        }

        var delay = await _service.GetProgressiveDelayAsync(ipAddress);

        // Assert
        delay.Should().BeLessThanOrEqualTo(30000); // Max 30 seconds
    }

    #endregion

    #region GetRemainingLockoutSecondsAsync Tests

    [Fact]
    public async Task GetRemainingLockoutSecondsAsync_NotLocked_ShouldReturnZero()
    {
        // Arrange
        var ipAddress = "192.168.1.12";

        // Act
        var remainingSeconds = await _service.GetRemainingLockoutSecondsAsync(ipAddress);

        // Assert
        remainingSeconds.Should().Be(0);
    }

    [Fact]
    public async Task GetRemainingLockoutSecondsAsync_AfterLockout_ShouldReturnPositive()
    {
        // Arrange
        var ipAddress = "192.168.1.13";
        var email = "test@example.com";

        // Act - Trigger lockout
        for (int i = 0; i < 10; i++)
        {
            await _service.RecordFailedAttemptAsync(ipAddress, email);
        }

        var remainingSeconds = await _service.GetRemainingLockoutSecondsAsync(ipAddress);

        // Assert
        remainingSeconds.Should().BeGreaterThan(0);
        remainingSeconds.Should().BeLessThanOrEqualTo(15 * 60);
    }

    #endregion

    #region GetFailedAttemptCountAsync Tests

    [Fact]
    public async Task GetFailedAttemptCountAsync_NoAttempts_ShouldReturnZero()
    {
        // Arrange
        var ipAddress = "192.168.1.14";

        // Act
        var count = await _service.GetFailedAttemptCountAsync(ipAddress);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetFailedAttemptCountAsync_WithAttempts_ShouldReturnCorrectCount()
    {
        // Arrange
        var ipAddress = "192.168.1.15";

        // Act
        await _service.RecordFailedAttemptAsync(ipAddress, "test1@example.com");
        await _service.RecordFailedAttemptAsync(ipAddress, "test2@example.com");
        await _service.RecordFailedAttemptAsync(ipAddress, "test3@example.com");
        await _service.RecordFailedAttemptAsync(ipAddress, "test4@example.com");
        await _service.RecordFailedAttemptAsync(ipAddress, "test5@example.com");

        var count = await _service.GetFailedAttemptCountAsync(ipAddress);

        // Assert
        count.Should().Be(5);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullLoginFlow_FailedThenSuccess_ShouldResetState()
    {
        // Arrange
        var ipAddress = "192.168.1.20";
        var email = "user@example.com";

        // Act - Multiple failed attempts
        for (int i = 0; i < 5; i++)
        {
            await _service.RecordFailedAttemptAsync(ipAddress, email);
        }

        var countBeforeSuccess = await _service.GetFailedAttemptCountAsync(ipAddress);
        var delayBeforeSuccess = await _service.GetProgressiveDelayAsync(ipAddress);

        // Successful login
        await _service.RecordSuccessfulLoginAsync(ipAddress, email);

        var countAfterSuccess = await _service.GetFailedAttemptCountAsync(ipAddress);
        var delayAfterSuccess = await _service.GetProgressiveDelayAsync(ipAddress);

        // Assert
        countBeforeSuccess.Should().Be(5);
        delayBeforeSuccess.Should().BeGreaterThan(0);

        countAfterSuccess.Should().Be(0);
        delayAfterSuccess.Should().Be(0);
    }

    [Fact]
    public async Task DifferentIps_ShouldBeTrackedSeparately()
    {
        // Arrange
        var ip1 = "10.0.0.1";
        var ip2 = "10.0.0.2";
        var email = "test@example.com";

        // Act
        await _service.RecordFailedAttemptAsync(ip1, email);
        await _service.RecordFailedAttemptAsync(ip1, email);
        await _service.RecordFailedAttemptAsync(ip1, email);
        await _service.RecordFailedAttemptAsync(ip2, email);

        var count1 = await _service.GetFailedAttemptCountAsync(ip1);
        var count2 = await _service.GetFailedAttemptCountAsync(ip2);

        // Assert
        count1.Should().Be(3);
        count2.Should().Be(1);
    }

    #endregion
}
