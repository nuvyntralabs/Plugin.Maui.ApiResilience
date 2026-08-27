namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Circuit-breaker policy that fails fast after a burst of errors.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Enables the circuit breaker. Default is <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How circuit state is partitioned. Default is <see cref="CircuitScope.PerHost"/>.
    /// </summary>
    public CircuitScope Scope { get; set; } = CircuitScope.PerHost;

    /// <summary>
    /// Failure ratio (0–1) that opens the circuit. Default is 0.5.
    /// </summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Minimum calls in the sampling window before the circuit can open. Default is 8.
    /// </summary>
    public int MinimumThroughput { get; set; } = 8;

    /// <summary>
    /// Window used to calculate the failure ratio. Default is 30 seconds.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the circuit stays open before a trial call is allowed. Default is 15 seconds.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// HTTP status codes that count as failures toward opening the circuit.
    /// </summary>
    public ISet<HttpStatusCode> FailureStatusCodes { get; } = new HashSet<HttpStatusCode>
    {
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    };
}
