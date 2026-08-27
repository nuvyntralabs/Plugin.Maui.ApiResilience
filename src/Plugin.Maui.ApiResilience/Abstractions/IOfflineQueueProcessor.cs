namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Replays persisted requests when connectivity is available.
/// </summary>
public interface IOfflineQueueProcessor
{
    /// <summary>
    /// <see langword="true"/> while a replay pass is running.
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Replays pending requests now. Safe to call concurrently; overlapping calls share one pass.
    /// </summary>
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
