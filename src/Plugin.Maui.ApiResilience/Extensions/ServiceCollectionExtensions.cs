namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Registers shared services used by the resilience pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds connectivity, offline queue, and replay hosted services.
    /// Call <see cref="HttpClientBuilderExtensions.AddApiResilience(IHttpClientBuilder, Action{ApiResilienceOptions}?)"/>
    /// on each <see cref="HttpClient"/> that should use the pipeline.
    /// </summary>
    public static IServiceCollection AddApiResilience(this IServiceCollection services, Action<ApiResilienceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ApiResilienceOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IConnectivityProvider, MauiConnectivityProvider>();
        services.TryAddSingleton<IAppStorage, MauiAppStorage>();
        services.TryAddSingleton<IOfflineRequestQueue, FileOfflineRequestQueue>();
        services.TryAddSingleton<IOfflineQueueProcessor, OfflineQueueProcessor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, OfflineQueueHostedService>());

        return services;
    }
}
