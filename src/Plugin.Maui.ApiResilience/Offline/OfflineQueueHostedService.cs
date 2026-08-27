namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Starts queue replay on launch and whenever connectivity returns.
/// </summary>
public sealed class OfflineQueueHostedService : IHostedService, IDisposable
{
    private readonly IConnectivityProvider _connectivity;
    private readonly IOfflineQueueProcessor _processor;
    private readonly IOptionsMonitor<ApiResilienceOptions> _options;
    private readonly ILogger<OfflineQueueHostedService> _logger;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Creates the hosted service.
    /// </summary>
    public OfflineQueueHostedService(
        IConnectivityProvider connectivity,
        IOfflineQueueProcessor processor,
        IOptionsMonitor<ApiResilienceOptions> options,
        ILogger<OfflineQueueHostedService>? logger = null)
    {
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<OfflineQueueHostedService>.Instance;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _connectivity.ConnectivityChanged += OnConnectivityChanged;

        if (_options.CurrentValue.OfflineQueue is { Enabled: true, ReplayOnReconnect: true } && _connectivity.IsConnected)
        {
            _ = SafeProcessAsync(_cts.Token);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _cts?.Dispose();
    }

    private void OnConnectivityChanged(object? sender, NetworkAccessChangedEventArgs e)
    {
        if (!e.IsConnected || !_options.CurrentValue.OfflineQueue.Enabled || !_options.CurrentValue.OfflineQueue.ReplayOnReconnect)
        {
            return;
        }

        _logger.LogInformation("Connectivity restored. Replaying the offline HTTP queue.");
        _ = SafeProcessAsync(_cts?.Token ?? CancellationToken.None);
    }

    private async Task SafeProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _processor.ProcessAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Offline queue replay failed.");
        }
    }
}
