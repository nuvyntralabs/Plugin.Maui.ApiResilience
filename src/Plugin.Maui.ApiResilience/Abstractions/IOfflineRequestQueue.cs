namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Durable store for HTTP requests that could not be delivered.
/// </summary>
public interface IOfflineRequestQueue
{
    /// <summary>
    /// Number of pending (not dead-lettered) requests.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pending requests in enqueue order.
    /// </summary>
    Task<IReadOnlyList<QueuedRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests that exhausted replay attempts.
    /// </summary>
    Task<IReadOnlyList<QueuedRequest>> GetDeadLettersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a request. Returns the assigned identifier.
    /// </summary>
    Task<string> EnqueueAsync(QueuedRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a pending request (for example after incrementing <see cref="QueuedRequest.Attempts"/>).
    /// </summary>
    Task UpdateAsync(QueuedRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pending request after a successful replay.
    /// </summary>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a pending request to the dead-letter store.
    /// </summary>
    Task DeadLetterAsync(QueuedRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every pending and dead-lettered request.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
