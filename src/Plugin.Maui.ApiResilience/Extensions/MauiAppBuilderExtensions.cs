using Microsoft.Maui.Hosting;

namespace Plugin.Maui.ApiResilience;

/// <summary>
/// MAUI host registration for API resilience.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers resilience services and a default named <see cref="HttpClient"/>
    /// (<see cref="ApiResilienceDefaults.HttpClientName"/>).
    /// Configure each additional client with <c>AddHttpClient(...).AddApiResilience()</c>.
    /// </summary>
    public static MauiAppBuilder UseApiResilience(this MauiAppBuilder builder, Action<ApiResilienceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddApiResilience(configure);
        builder.Services
            .AddHttpClient(ApiResilienceDefaults.HttpClientName)
            .AddApiResilience();

        return builder;
    }
}
