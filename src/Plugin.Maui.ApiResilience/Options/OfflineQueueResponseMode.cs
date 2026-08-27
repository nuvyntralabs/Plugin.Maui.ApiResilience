namespace Plugin.Maui.ApiResilience;

/// <summary>
/// How the handler responds to the caller after a request is persisted offline.
/// </summary>
public enum OfflineQueueResponseMode
{
    /// <summary>
    /// Throw <see cref="RequestQueuedOfflineException"/> so the caller can show a "will sync" state.
    /// </summary>
    ThrowException = 0,

    /// <summary>
    /// Return HTTP 202 Accepted with an <c>X-ApiResilience-Queued</c> header.
    /// </summary>
    AcceptedResponse = 1
}
