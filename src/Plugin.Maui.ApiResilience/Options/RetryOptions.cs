namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Retry policy applied to transient HTTP failures.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Enables the retry strategy. Default is <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of retry attempts after the first call. Default is 3 (4 total attempts).
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries. Default is 2 seconds.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Optional ceiling for exponential backoff.
    /// </summary>
    public TimeSpan? MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff algorithm. Default is exponential.
    /// </summary>
    public DelayBackoffType BackoffType { get; set; } = DelayBackoffType.Exponential;

    /// <summary>
    /// Adds random jitter to the delay to reduce thundering herds. Default is <see langword="true"/>.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// HTTP status codes that are retried. 401 is intentionally omitted; token refresh handles it.
    /// </summary>
    public ISet<HttpStatusCode> TransientStatusCodes { get; } = new HashSet<HttpStatusCode>
    {
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    };
}
