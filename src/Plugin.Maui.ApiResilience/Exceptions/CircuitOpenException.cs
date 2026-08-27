namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Thrown when the circuit breaker is open and the call is rejected without reaching the network.
/// </summary>
public sealed class CircuitOpenException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    public CircuitOpenException(string scopeKey, TimeSpan breakDuration, Exception innerException)
        : base($"Circuit '{scopeKey}' is open. Fast-failing for {breakDuration}.", innerException)
    {
        ScopeKey = scopeKey;
        BreakDuration = breakDuration;
    }

    /// <summary>
    /// Host name or <c>global</c>.
    /// </summary>
    public string ScopeKey { get; }

    /// <summary>
    /// Configured open duration.
    /// </summary>
    public TimeSpan BreakDuration { get; }
}
