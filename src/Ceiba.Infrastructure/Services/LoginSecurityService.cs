using Ceiba.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// In-memory implementation of login security service with rate limiting and progressive delays.
/// For production deployments with multiple instances, consider using Redis or database-backed storage.
/// </summary>
public class LoginSecurityService : ILoginSecurityService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LoginSecurityService> _logger;

    // Configuration constants - could be moved to appsettings.json
    private const int MaxFailedAttemptsBeforeDelay = 3;
    private const int MaxFailedAttemptsBeforeLockout = 10;
    private const int LockoutDurationMinutes = 15;
    private const int AttemptWindowMinutes = 30;
    private const int BaseDelayMs = 1000;
    private const int MaxDelayMs = 30000;

    public LoginSecurityService(IMemoryCache cache, ILogger<LoginSecurityService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<bool> IsIpBlockedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var lockoutKey = GetLockoutKey(ipAddress);
        var isBlocked = _cache.TryGetValue(lockoutKey, out DateTime lockoutEnd) && lockoutEnd > DateTime.UtcNow;

        if (isBlocked)
        {
            _logger.LogWarning("IP {IpAddress} is currently blocked until {LockoutEnd}", ipAddress, lockoutEnd);
        }

        return Task.FromResult(isBlocked);
    }

    public Task RecordFailedAttemptAsync(string ipAddress, string email, CancellationToken cancellationToken = default)
    {
        var attemptsKey = GetAttemptsKey(ipAddress);
        var attempts = GetOrCreateAttemptRecord(attemptsKey);

        attempts.Count++;
        attempts.LastAttempt = DateTime.UtcNow;
        attempts.Emails.Add(email);

        // Set cache with sliding expiration
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(AttemptWindowMinutes));
        _cache.Set(attemptsKey, attempts, cacheOptions);

        _logger.LogWarning(
            "Failed login attempt {Count} from IP {IpAddress} for email {Email}",
            attempts.Count, ipAddress, email);

        // Check if we should lock out this IP
        if (attempts.Count >= MaxFailedAttemptsBeforeLockout)
        {
            var lockoutKey = GetLockoutKey(ipAddress);
            var lockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);

            _cache.Set(lockoutKey, lockoutEnd, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(lockoutEnd));

            _logger.LogWarning(
                "IP {IpAddress} has been locked out until {LockoutEnd} after {Count} failed attempts. Emails attempted: {Emails}",
                ipAddress, lockoutEnd, attempts.Count, string.Join(", ", attempts.Emails.Distinct()));
        }

        return Task.CompletedTask;
    }

    public Task RecordSuccessfulLoginAsync(string ipAddress, string email, CancellationToken cancellationToken = default)
    {
        var attemptsKey = GetAttemptsKey(ipAddress);
        _cache.Remove(attemptsKey);

        _logger.LogInformation("Successful login from IP {IpAddress} for email {Email}, clearing failed attempts", ipAddress, email);

        return Task.CompletedTask;
    }

    public Task<int> GetProgressiveDelayAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var attemptsKey = GetAttemptsKey(ipAddress);
        var attempts = GetOrCreateAttemptRecord(attemptsKey);

        if (attempts.Count < MaxFailedAttemptsBeforeDelay)
        {
            return Task.FromResult(0);
        }

        // Progressive delay: 1s, 2s, 4s, 8s, 16s, 30s max
        var delayMultiplier = attempts.Count - MaxFailedAttemptsBeforeDelay + 1;
        var delayMs = Math.Min(BaseDelayMs * (int)Math.Pow(2, delayMultiplier - 1), MaxDelayMs);

        _logger.LogDebug(
            "Progressive delay of {DelayMs}ms for IP {IpAddress} after {Count} failed attempts",
            delayMs, ipAddress, attempts.Count);

        return Task.FromResult(delayMs);
    }

    public Task<int> GetRemainingLockoutSecondsAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var lockoutKey = GetLockoutKey(ipAddress);

        if (_cache.TryGetValue(lockoutKey, out DateTime lockoutEnd) && lockoutEnd > DateTime.UtcNow)
        {
            var remainingSeconds = (int)(lockoutEnd - DateTime.UtcNow).TotalSeconds;
            return Task.FromResult(remainingSeconds);
        }

        return Task.FromResult(0);
    }

    public Task<int> GetFailedAttemptCountAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var attemptsKey = GetAttemptsKey(ipAddress);
        var attempts = GetOrCreateAttemptRecord(attemptsKey);
        return Task.FromResult(attempts.Count);
    }

    private static string GetAttemptsKey(string ipAddress) => $"login_attempts:{ipAddress}";
    private static string GetLockoutKey(string ipAddress) => $"login_lockout:{ipAddress}";

    private LoginAttemptRecord GetOrCreateAttemptRecord(string key)
    {
        if (!_cache.TryGetValue(key, out LoginAttemptRecord? record) || record == null)
        {
            record = new LoginAttemptRecord();
        }
        return record;
    }

    private class LoginAttemptRecord
    {
        public int Count { get; set; }
        public DateTime LastAttempt { get; set; }
        public HashSet<string> Emails { get; set; } = [];
    }
}
