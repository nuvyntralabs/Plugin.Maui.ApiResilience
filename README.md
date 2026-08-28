# Plugin.Maui.ApiResilience

HTTP resilience for **.NET MAUI** on **iOS** and **Android**.

The package wraps `HttpClient` with four complementary behaviors:

| Feature | What it does |
| --- | --- |
| **Retry** | Retries transient HTTP failures (408, 429, 5xx) and transport errors with exponential backoff and jitter |
| **Circuit breaker** | Fails fast after a burst of errors, per host by default |
| **Offline queue** | Persists POST/PUT/PATCH/DELETE when the device is offline and replays them on reconnect |
| **Token refresh** | Attaches a bearer token and refreshes it once on 401, with single-flight refresh |

## Install

```bash
dotnet add package Plugin.Maui.ApiResilience
```

## Quick start

```csharp
using Plugin.Maui.ApiResilience;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseApiResilience(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
                options.OfflineQueue.Enabled = true;

                options.TokenRefresh.Enabled = true;
            });

        builder.Services.AddSingleton<IAccessTokenProvider, AuthTokenProvider>();

        builder.Services
            .AddHttpClient<ICatalogApi, CatalogApi>(client =>
            {
                client.BaseAddress = new Uri("https://api.example.com");
            })
            .AddApiResilience();

        return builder.Build();
    }
}
```

Resolve `ICatalogApi` (or `IHttpClientFactory`) as usual. Every call goes through the pipeline.

## Token refresh

Register `IAccessTokenProvider` **or** set the delegates on `TokenRefreshOptions`.

```csharp
public sealed class AuthTokenProvider : IAccessTokenProvider
{
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => SecureStorage.Default.GetAsync("access_token");

    public async Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Call your auth endpoint, persist tokens, return the new access token.
        var refreshed = await authApi.RefreshAsync(cancellationToken);
        await SecureStorage.Default.SetAsync("access_token", refreshed.AccessToken);
        return refreshed.AccessToken;
    }
}
```

Concurrent 401s share one refresh. If another request already refreshed the token, the original request is retried with the new value and the refresh endpoint is not called again.

Authorization headers are **not** written to the offline queue file.

## Offline queue

When `Connectivity` reports no internet, queueable methods are stored under `FileSystem.AppDataDirectory` and replayed when connectivity returns (and on app start).

```csharp
options.OfflineQueue.Enabled = true;
options.OfflineQueue.MaxQueueSize = 100;
options.OfflineQueue.ReplayOnReconnect = true;
options.OfflineQueue.ResponseMode = OfflineQueueResponseMode.ThrowException;
```

`ThrowException` (default) raises `RequestQueuedOfflineException` so the UI can show a “will sync” state. `AcceptedResponse` returns HTTP 202 with `X-ApiResilience-Queued: true`.

Inspect or flush the queue:

```csharp
var queue = handler.Services.GetRequiredService<IOfflineRequestQueue>();
var pending = await queue.GetPendingAsync();

var processor = handler.Services.GetRequiredService<IOfflineQueueProcessor>();
await processor.ProcessAsync();
```

GET/HEAD/OPTIONS are not queued by default.

## Retry and circuit breaker

```csharp
options.Retry.MaxRetryAttempts = 3;
options.Retry.Delay = TimeSpan.FromSeconds(2);
options.Retry.BackoffType = DelayBackoffType.Exponential;
options.Retry.UseJitter = true;

options.CircuitBreaker.Scope = CircuitScope.PerHost;
options.CircuitBreaker.FailureRatio = 0.5;
options.CircuitBreaker.MinimumThroughput = 8;
options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

options.Timeout.RequestTimeout = TimeSpan.FromSeconds(30);
```

An open circuit throws `CircuitOpenException` instead of waiting on a failing host.

## Events

```csharp
options.Events.OnRetry = e => Debug.WriteLine($"retry {e.AttemptNumber}");
options.Events.OnCircuitOpened = e => Debug.WriteLine($"circuit open {e.ScopeKey}");
options.Events.OnQueued = r => Debug.WriteLine($"queued {r.Id}");
options.Events.OnTokenRefreshed = () => Debug.WriteLine("token refreshed");
```

## Without the generic host

```csharp
var client = ApiResilienceHttp.CreateClient(options =>
{
    options.Retry.MaxRetryAttempts = 2;
    options.TokenRefresh.Enabled = true;
    options.TokenRefresh.GetAccessTokenAsync = ct => SecureStorage.Default.GetAsync("access_token");
    options.TokenRefresh.RefreshAccessTokenAsync = ct => RefreshAsync(ct);
});
```

## Target frameworks

The package targets `net10.0`, `net10.0-android`, and `net10.0-ios`.

## Pack from source

```bash
dotnet pack src/Plugin.Maui.ApiResilience/Plugin.Maui.ApiResilience.csproj -c Release -o artifacts
```

The `.nupkg` is written to `artifacts/Plugin.Maui.ApiResilience.1.0.0.nupkg`.

## License

MIT

## Support

> If this plugin saved you a weekend of native plumbing, consider buying me a coffee.
> Your support keeps it maintained, documented, and free.

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/npadhy)

This library stays open source. A coffee helps cover time for bug fixes, new features, and docs.
