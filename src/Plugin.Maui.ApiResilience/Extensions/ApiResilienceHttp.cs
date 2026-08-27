namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Builds a resilient <see cref="HttpClient"/> without the MAUI generic host.
/// Prefer <c>UseApiResilience</c> plus <c>IHttpClientFactory</c> in a MAUI app.
/// </summary>
public static class ApiResilienceHttp
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> whose handler applies retry, circuit breaker,
    /// offline queue, and token refresh.
    /// </summary>
    public static HttpClient CreateClient(Action<ApiResilienceOptions>? configure = null, HttpMessageHandler? innerHandler = null)
    {
        var options = new ApiResilienceOptions();
        configure?.Invoke(options);
        return CreateClient(options, innerHandler);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> using the supplied options.
    /// </summary>
    public static HttpClient CreateClient(ApiResilienceOptions options, HttpMessageHandler? innerHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        IAccessTokenProvider? tokens = options.TokenRefresh.Enabled
            ? new DelegateAccessTokenProvider(options.TokenRefresh)
            : null;

        var connectivity = new MauiConnectivityProvider();
        var queue = new FileOfflineRequestQueue(
            new MauiAppStorage(),
            new StaticOptionsMonitor<ApiResilienceOptions>(options));

        var handler = new ApiResilienceHandler(
            options,
            connectivity,
            queue,
            tokens,
            NullLogger<ApiResilienceHandler>.Instance,
            ApiResilienceDefaults.HttpClientName)
        {
            InnerHandler = innerHandler ?? new HttpClientHandler()
        };

        var client = new HttpClient(handler, disposeHandler: true);

        if (options.OfflineQueue is { Enabled: true, ReplayOnReconnect: true })
        {
            var processor = new OfflineQueueProcessor(
                queue,
                new StaticOptionsMonitor<ApiResilienceOptions>(options),
                httpClientFactory: null,
                fallbackClient: client);

            connectivity.ConnectivityChanged += (_, e) =>
            {
                if (e.IsConnected)
                {
                    _ = processor.ProcessAsync();
                }
            };
        }

        return client;
    }
}
