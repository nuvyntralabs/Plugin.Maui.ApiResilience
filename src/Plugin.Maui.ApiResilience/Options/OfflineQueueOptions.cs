namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Persists mutating HTTP calls while the device is offline and replays them on reconnect.
/// </summary>
public sealed class OfflineQueueOptions
{
    /// <summary>
    /// Enables the offline queue. Default is <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Also queue after a transport failure (for example <see cref="HttpRequestException"/>) when the device still reports connectivity.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool QueueOnTransportFailure { get; set; } = true;

    /// <summary>
    /// Replay stored requests automatically when connectivity returns. Default is <see langword="true"/>.
    /// </summary>
    public bool ReplayOnReconnect { get; set; } = true;

    /// <summary>
    /// Maximum pending requests. Default is 100.
    /// </summary>
    public int MaxQueueSize { get; set; } = 100;

    /// <summary>
    /// How many times a queued request is retried during replay before it is dead-lettered. Default is 5.
    /// </summary>
    public int MaxReplayAttempts { get; set; } = 5;

    /// <summary>
    /// Behavior when <see cref="MaxQueueSize"/> is reached. Default is <see cref="OfflineQueueOverflowBehavior.DropOldest"/>.
    /// </summary>
    public OfflineQueueOverflowBehavior OverflowBehavior { get; set; } = OfflineQueueOverflowBehavior.DropOldest;

    /// <summary>
    /// How the original caller is notified after a request is queued. Default is <see cref="OfflineQueueResponseMode.ThrowException"/>.
    /// </summary>
    public OfflineQueueResponseMode ResponseMode { get; set; } = OfflineQueueResponseMode.ThrowException;

    /// <summary>
    /// HTTP methods that may be persisted. Safe methods (GET, HEAD, OPTIONS) are excluded by default.
    /// </summary>
    public ISet<HttpMethod> QueueableMethods { get; } = new HashSet<HttpMethod>
    {
        HttpMethod.Post,
        HttpMethod.Put,
        HttpMethod.Patch,
        HttpMethod.Delete
    };
}
