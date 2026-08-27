namespace Plugin.Maui.ApiResilience;

/// <summary>
/// A persisted HTTP request waiting to be replayed.
/// </summary>
public sealed class QueuedRequest
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method name (POST, PUT, …).
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Absolute request URI.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Non-sensitive headers.
    /// </summary>
    public Dictionary<string, string[]> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Request body encoded as Base64, or <see langword="null"/> when empty.
    /// </summary>
    public string? ContentBase64 { get; set; }

    /// <summary>
    /// Original <c>Content-Type</c> header.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// When the request was first queued.
    /// </summary>
    public DateTimeOffset EnqueuedAtUtc { get; set; }

    /// <summary>
    /// Replay attempts already performed.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Named HttpClient that should replay the request.
    /// </summary>
    public string? HttpClientName { get; set; }

    /// <summary>
    /// Last error recorded during replay, if any.
    /// </summary>
    public string? LastError { get; set; }
}
