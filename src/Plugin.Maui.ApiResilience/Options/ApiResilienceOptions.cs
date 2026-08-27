namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Root configuration for <c>Plugin.Maui.ApiResilience</c>.
/// </summary>
public sealed class ApiResilienceOptions
{
    /// <summary>
    /// Retry policy.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Circuit-breaker policy.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Overall request timeout.
    /// </summary>
    public TimeoutOptions Timeout { get; set; } = new();

    /// <summary>
    /// Offline persistence and replay.
    /// </summary>
    public OfflineQueueOptions OfflineQueue { get; set; } = new();

    /// <summary>
    /// Bearer token attachment and 401 refresh.
    /// </summary>
    public TokenRefreshOptions TokenRefresh { get; set; } = new();

    /// <summary>
    /// Diagnostic callbacks.
    /// </summary>
    public ApiResilienceEvents Events { get; set; } = new();
}
