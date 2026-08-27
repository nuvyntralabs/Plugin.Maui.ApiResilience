namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Thrown when a request is stored for later replay because the device is offline
/// or the transport failed, and <see cref="OfflineQueueResponseMode.ThrowException"/> is selected.
/// </summary>
public sealed class RequestQueuedOfflineException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    public RequestQueuedOfflineException(string requestId, Uri? requestUri, HttpMethod method)
        : base($"Request {method} {requestUri} was queued offline as '{requestId}'.")
    {
        RequestId = requestId;
        RequestUri = requestUri;
        Method = method;
    }

    /// <summary>
    /// Identifier of the persisted request.
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// Target URI.
    /// </summary>
    public Uri? RequestUri { get; }

    /// <summary>
    /// HTTP method.
    /// </summary>
    public HttpMethod Method { get; }
}
