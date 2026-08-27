namespace Plugin.Maui.ApiResilience;

/// <summary>
/// What to do when the offline queue is already at <see cref="OfflineQueueOptions.MaxQueueSize"/>.
/// </summary>
public enum OfflineQueueOverflowBehavior
{
    /// <summary>
    /// Discard the oldest pending request to make room.
    /// </summary>
    DropOldest = 0,

    /// <summary>
    /// Reject the new request and do not queue it.
    /// </summary>
    RejectNew = 1
}
