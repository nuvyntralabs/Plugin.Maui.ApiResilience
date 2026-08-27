using System.Net;
using Plugin.Maui.ApiResilience;

namespace Plugin.Maui.ApiResilience.Tests;

internal sealed class FakeConnectivity : IConnectivityProvider
{
    public bool IsConnected { get; set; } = true;

    public event EventHandler<NetworkAccessChangedEventArgs>? ConnectivityChanged;

    public void SetConnected(bool connected)
    {
        IsConnected = connected;
        ConnectivityChanged?.Invoke(this, new NetworkAccessChangedEventArgs(connected));
    }
}

internal sealed class FakeTokenProvider : IAccessTokenProvider
{
    public string? AccessToken { get; set; } = "access-1";

    public string? RefreshTokenValue { get; set; } = "access-2";

    public int RefreshCalls { get; private set; }

    public Func<CancellationToken, Task<string?>>? RefreshImpl { get; set; }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AccessToken);

    public async Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        RefreshCalls++;
        if (RefreshImpl is not null)
        {
            AccessToken = await RefreshImpl(cancellationToken);
            return AccessToken;
        }

        AccessToken = RefreshTokenValue;
        return AccessToken;
    }
}

internal sealed class ScriptedHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> _script;

    public ScriptedHandler(Func<HttpRequestMessage, int, HttpResponseMessage> script)
        : this((request, call, _) => Task.FromResult(script(request, call)))
    {
    }

    public ScriptedHandler(Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> script)
    {
        _script = script;
    }

    public int Calls { get; private set; }

    public List<string?> AuthorizationHeaders { get; } = [];

    public List<string?> Bodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
        if (request.Content is not null)
        {
            Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }
        else
        {
            Bodies.Add(null);
        }

        return await _script(request, Calls, cancellationToken);
    }
}

internal static class HandlerFactory
{
    public static ApiResilienceHandler Create(
        ApiResilienceOptions options,
        HttpMessageHandler inner,
        IConnectivityProvider? connectivity = null,
        IOfflineRequestQueue? queue = null,
        IAccessTokenProvider? tokens = null)
    {
        var handler = new ApiResilienceHandler(
            options,
            connectivity ?? new FakeConnectivity(),
            queue ?? new InMemoryOfflineRequestQueue(),
            tokens,
            logger: null,
            httpClientName: "tests")
        {
            InnerHandler = inner
        };

        return handler;
    }

    public static HttpClient CreateClient(
        ApiResilienceOptions options,
        HttpMessageHandler inner,
        IConnectivityProvider? connectivity = null,
        IOfflineRequestQueue? queue = null,
        IAccessTokenProvider? tokens = null)
    {
        return new HttpClient(Create(options, inner, connectivity, queue, tokens), disposeHandler: true)
        {
            BaseAddress = new Uri("https://api.test.local")
        };
    }

    public static ApiResilienceOptions FastRetry()
    {
        var options = new ApiResilienceOptions();
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        options.Retry.MaxRetryAttempts = 3;
        options.Timeout.Enabled = false;
        options.CircuitBreaker.Enabled = false;
        options.OfflineQueue.Enabled = false;
        options.TokenRefresh.Enabled = false;
        return options;
    }
}
