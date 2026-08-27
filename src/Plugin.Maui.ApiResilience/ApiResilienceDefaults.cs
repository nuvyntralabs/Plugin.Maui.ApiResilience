namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Shared keys and defaults used by the resilience pipeline.
/// </summary>
public static class ApiResilienceDefaults
{
    /// <summary>
    /// Default name used by <c>IHttpClientFactory</c> when no name is specified.
    /// </summary>
    public const string HttpClientName = "ApiResilience";

    /// <summary>
    /// <see cref="HttpRequestOptionsKey{TValue}"/> that marks a request as an offline-queue replay.
    /// </summary>
    public static readonly HttpRequestOptionsKey<bool> IsReplayKey = new("Plugin.Maui.ApiResilience.IsReplay");

    /// <summary>
    /// <see cref="HttpRequestOptionsKey{TValue}"/> that stores the named HttpClient that originated the request.
    /// </summary>
    public static readonly HttpRequestOptionsKey<string> HttpClientNameKey = new("Plugin.Maui.ApiResilience.HttpClientName");

    internal const string QueueFileName = "plugin.maui.apiresilience.queue.json";
}
