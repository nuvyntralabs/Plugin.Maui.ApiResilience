using Microsoft.Extensions.Logging;
using Plugin.Maui.ApiResilience;
using Plugin.Maui.ApiResilience.Sample.Demo;

namespace Plugin.Maui.ApiResilience.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.Services.AddSingleton<DemoConnectivity>();
        builder.Services.AddSingleton<IConnectivityProvider>(sp => sp.GetRequiredService<DemoConnectivity>());
        builder.Services.AddSingleton<DemoTokenProvider>();
        builder.Services.AddSingleton<IAccessTokenProvider>(sp => sp.GetRequiredService<DemoTokenProvider>());
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseApiResilience(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(200);
                options.Retry.UseJitter = false;
                options.Timeout.Enabled = false;

                options.CircuitBreaker.MinimumThroughput = 4;
                options.CircuitBreaker.FailureRatio = 1;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);

                options.OfflineQueue.Enabled = true;
                options.OfflineQueue.ResponseMode = OfflineQueueResponseMode.ThrowException;

                options.TokenRefresh.Enabled = true;
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddHttpClient("demo", client => client.BaseAddress = new Uri("https://demo.local/"))
            .ConfigurePrimaryHttpMessageHandler(() => new DemoApiHandler())
            .AddApiResilience();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
