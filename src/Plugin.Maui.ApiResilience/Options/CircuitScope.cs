namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Controls how circuit-breaker state is partitioned.
/// </summary>
public enum CircuitScope
{
    /// <summary>
    /// One circuit is shared by every request.
    /// </summary>
    Global = 0,

    /// <summary>
    /// A separate circuit is maintained per request host.
    /// </summary>
    PerHost = 1
}
