namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Attaches <see cref="ApiResilienceHandler"/> to an <see cref="IHttpClientBuilder"/>.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds retry, circuit breaker, timeout, offline queue, and token refresh to this client.
    /// When <paramref name="configure"/> is omitted, the options from
    /// <see cref="ServiceCollectionExtensions.AddApiResilience"/> / <c>UseApiResilience</c> are used.
    /// </summary>
    public static IHttpClientBuilder AddApiResilience(
        this IHttpClientBuilder builder,
        Action<ApiResilienceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddApiResilience();

        builder.AddHttpMessageHandler(sp =>
        {
            var options = ResolveOptions(sp, configure);
            return new ApiResilienceHandler(
                options,
                sp.GetRequiredService<IConnectivityProvider>(),
                sp.GetRequiredService<IOfflineRequestQueue>(),
                sp.GetService<IAccessTokenProvider>(),
                sp.GetService<ILogger<ApiResilienceHandler>>(),
                builder.Name);
        });

        return builder;
    }

    private static ApiResilienceOptions ResolveOptions(IServiceProvider sp, Action<ApiResilienceOptions>? configure)
    {
        var defaults = sp.GetRequiredService<IOptionsMonitor<ApiResilienceOptions>>().CurrentValue;
        if (configure is null)
        {
            return defaults;
        }

        var options = Clone(defaults);
        configure(options);
        return options;
    }

    private static ApiResilienceOptions Clone(ApiResilienceOptions source)
    {
        var copy = new ApiResilienceOptions
        {
            Events = source.Events,
            Retry =
            {
                Enabled = source.Retry.Enabled,
                MaxRetryAttempts = source.Retry.MaxRetryAttempts,
                Delay = source.Retry.Delay,
                MaxDelay = source.Retry.MaxDelay,
                BackoffType = source.Retry.BackoffType,
                UseJitter = source.Retry.UseJitter
            },
            CircuitBreaker =
            {
                Enabled = source.CircuitBreaker.Enabled,
                Scope = source.CircuitBreaker.Scope,
                FailureRatio = source.CircuitBreaker.FailureRatio,
                MinimumThroughput = source.CircuitBreaker.MinimumThroughput,
                SamplingDuration = source.CircuitBreaker.SamplingDuration,
                BreakDuration = source.CircuitBreaker.BreakDuration
            },
            Timeout =
            {
                Enabled = source.Timeout.Enabled,
                RequestTimeout = source.Timeout.RequestTimeout
            },
            OfflineQueue =
            {
                Enabled = source.OfflineQueue.Enabled,
                QueueOnTransportFailure = source.OfflineQueue.QueueOnTransportFailure,
                ReplayOnReconnect = source.OfflineQueue.ReplayOnReconnect,
                MaxQueueSize = source.OfflineQueue.MaxQueueSize,
                MaxReplayAttempts = source.OfflineQueue.MaxReplayAttempts,
                OverflowBehavior = source.OfflineQueue.OverflowBehavior,
                ResponseMode = source.OfflineQueue.ResponseMode
            },
            TokenRefresh =
            {
                Enabled = source.TokenRefresh.Enabled,
                AuthenticationScheme = source.TokenRefresh.AuthenticationScheme,
                RethrowOnRefreshFailure = source.TokenRefresh.RethrowOnRefreshFailure,
                GetAccessTokenAsync = source.TokenRefresh.GetAccessTokenAsync,
                RefreshAccessTokenAsync = source.TokenRefresh.RefreshAccessTokenAsync
            }
        };

        copy.Retry.TransientStatusCodes.Clear();
        foreach (var code in source.Retry.TransientStatusCodes)
        {
            copy.Retry.TransientStatusCodes.Add(code);
        }

        copy.CircuitBreaker.FailureStatusCodes.Clear();
        foreach (var code in source.CircuitBreaker.FailureStatusCodes)
        {
            copy.CircuitBreaker.FailureStatusCodes.Add(code);
        }

        copy.OfflineQueue.QueueableMethods.Clear();
        foreach (var method in source.OfflineQueue.QueueableMethods)
        {
            copy.OfflineQueue.QueueableMethods.Add(method);
        }

        copy.TokenRefresh.UnauthorizedStatusCodes.Clear();
        foreach (var code in source.TokenRefresh.UnauthorizedStatusCodes)
        {
            copy.TokenRefresh.UnauthorizedStatusCodes.Add(code);
        }

        return copy;
    }
}
