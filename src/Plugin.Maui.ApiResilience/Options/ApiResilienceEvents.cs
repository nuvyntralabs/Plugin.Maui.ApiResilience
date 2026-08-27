namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Optional diagnostics hooks. Callbacks must be fast and exception-free.
/// </summary>
public sealed class ApiResilienceEvents
{
    /// <summary>
    /// Invoked before a retry sleep.
    /// </summary>
    public Action<RetryEvent>? OnRetry { get; set; }

    /// <summary>
    /// Invoked when a circuit opens.
    /// </summary>
    public Action<CircuitEvent>? OnCircuitOpened { get; set; }

    /// <summary>
    /// Invoked when a circuit returns to closed.
    /// </summary>
    public Action<CircuitEvent>? OnCircuitClosed { get; set; }

    /// <summary>
    /// Invoked when a circuit allows a trial call.
    /// </summary>
    public Action<CircuitEvent>? OnCircuitHalfOpened { get; set; }

    /// <summary>
    /// Invoked when the overall request timeout fires.
    /// </summary>
    public Action<TimeoutEvent>? OnTimeout { get; set; }

    /// <summary>
    /// Invoked after a request is persisted because the device is offline or the transport failed.
    /// </summary>
    public Action<QueuedRequest>? OnQueued { get; set; }

    /// <summary>
    /// Invoked after a queued request is replayed. The response argument may be <see langword="null"/> on transport failure.
    /// </summary>
    public Action<QueuedRequest, HttpResponseMessage?>? OnReplayed { get; set; }

    /// <summary>
    /// Invoked after a queued request is moved to the dead-letter store.
    /// </summary>
    public Action<QueuedRequest>? OnDeadLettered { get; set; }

    /// <summary>
    /// Invoked after a token refresh succeeds.
    /// </summary>
    public Action? OnTokenRefreshed { get; set; }

    /// <summary>
    /// Invoked when token refresh throws.
    /// </summary>
    public Action<Exception>? OnTokenRefreshFailed { get; set; }
}

/// <summary>
/// Retry diagnostic payload.
/// </summary>
/// <param name="AttemptNumber">Zero-based retry attempt reported by Polly.</param>
/// <param name="Delay">Sleep before the next call.</param>
/// <param name="StatusCode">Status from the failed attempt, when available.</param>
/// <param name="Exception">Exception from the failed attempt, when available.</param>
public sealed record RetryEvent(int AttemptNumber, TimeSpan Delay, HttpStatusCode? StatusCode, Exception? Exception);

/// <summary>
/// Circuit-breaker diagnostic payload.
/// </summary>
/// <param name="ScopeKey">Host or <c>global</c>.</param>
/// <param name="BreakDuration">Configured open duration, when known.</param>
public sealed record CircuitEvent(string ScopeKey, TimeSpan? BreakDuration);

/// <summary>
/// Timeout diagnostic payload.
/// </summary>
/// <param name="Timeout">Configured timeout.</param>
public sealed record TimeoutEvent(TimeSpan Timeout);
