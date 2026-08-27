using Plugin.Maui.ApiResilience;
using Plugin.Maui.ApiResilience.Sample.Demo;

namespace Plugin.Maui.ApiResilience.Sample;

public partial class MainPage : ContentPage
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOfflineRequestQueue _queue;
    private readonly IOfflineQueueProcessor _processor;
    private readonly DemoConnectivity _connectivity;
    private readonly DemoTokenProvider _tokens;

    public MainPage(
        IHttpClientFactory httpClientFactory,
        IOfflineRequestQueue queue,
        IOfflineQueueProcessor processor,
        DemoConnectivity connectivity,
        DemoTokenProvider tokens)
    {
        InitializeComponent();
        _httpClientFactory = httpClientFactory;
        _queue = queue;
        _processor = processor;
        _connectivity = connectivity;
        _tokens = tokens;
    }

    private HttpClient Client => _httpClientFactory.CreateClient("demo");

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await RunAsync("Retry", () => Client.GetAsync("retry"));
    }

    private async void OnCircuitClicked(object? sender, EventArgs e)
    {
        try
        {
            HttpResponseMessage? last = null;
            for (var i = 0; i < 6; i++)
            {
                last = await Client.GetAsync("circuit");
                StatusLabel.Text = $"Circuit probe {i + 1}: {(int)last.StatusCode}";
            }

            StatusLabel.Text = $"Circuit still closed. Last status {(int)last!.StatusCode}.";
        }
        catch (CircuitOpenException ex)
        {
            StatusLabel.Text = ex.Message;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private async void OnTokenClicked(object? sender, EventArgs e)
    {
        _tokens.Expire();
        await RunAsync("Token refresh", async () =>
        {
            var response = await Client.GetAsync("secure");
            return response;
        });
        StatusLabel.Text += $"{Environment.NewLine}Refresh count: {_tokens.RefreshCount}. Token: {_tokens.AccessToken}";
    }

    private void OnToggleOfflineClicked(object? sender, EventArgs e)
    {
        _connectivity.SetForceOffline(!_connectivity.ForceOffline);
        OfflineToggleBtn.Text = _connectivity.ForceOffline ? "Simulate offline: ON" : "Simulate offline: OFF";
        StatusLabel.Text = _connectivity.IsConnected ? "Online." : "Offline. Mutating calls will be queued.";
    }

    private async void OnQueueClicked(object? sender, EventArgs e)
    {
        try
        {
            var response = await Client.PostAsync("queue", new StringContent("{\"item\":\"demo\"}"));
            StatusLabel.Text = $"POST completed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}";
        }
        catch (RequestQueuedOfflineException ex)
        {
            var count = await _queue.CountAsync();
            StatusLabel.Text = $"{ex.Message}{Environment.NewLine}Pending: {count}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private async void OnReplayClicked(object? sender, EventArgs e)
    {
        try
        {
            await _processor.ProcessAsync();
            var pending = await _queue.CountAsync();
            var dead = (await _queue.GetDeadLettersAsync()).Count;
            StatusLabel.Text = $"Replay finished. Pending: {pending}. Dead-letter: {dead}.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private async Task RunAsync(string title, Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            var body = await response.Content.ReadAsStringAsync();
            StatusLabel.Text = $"{title}: {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{body}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{title} failed: {ex.Message}";
        }
    }
}
