namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Replays persisted requests through <see cref="IHttpClientFactory"/> (or a fallback <see cref="HttpClient"/>).
/// </summary>
public sealed class OfflineQueueProcessor : IOfflineQueueProcessor
{
    private readonly IOfflineRequestQueue _queue;
    private readonly IOptionsMonitor<ApiResilienceOptions> _options;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly HttpClient? _fallbackClient;
    private readonly ILogger<OfflineQueueProcessor> _logger;
    private readonly SemaphoreSlim _runGate = new(1, 1);

    /// <summary>
    /// Creates the processor used by DI.
    /// </summary>
    public OfflineQueueProcessor(
        IOfflineRequestQueue queue,
        IOptionsMonitor<ApiResilienceOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<OfflineQueueProcessor>? logger = null)
        : this(queue, options, httpClientFactory, fallbackClient: null, logger)
    {
    }

    internal OfflineQueueProcessor(
        IOfflineRequestQueue queue,
        IOptionsMonitor<ApiResilienceOptions> options,
        IHttpClientFactory? httpClientFactory,
        HttpClient? fallbackClient,
        ILogger<OfflineQueueProcessor>? logger = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory;
        _fallbackClient = fallbackClient;
        _logger = logger ?? NullLogger<OfflineQueueProcessor>.Instance;
    }

    /// <inheritdoc />
    public bool IsProcessing { get; private set; }

    /// <inheritdoc />
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        if (!await _runGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        IsProcessing = true;
        try
        {
            var pending = await _queue.GetPendingAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReplayOneAsync(item, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            IsProcessing = false;
            _runGate.Release();
        }
    }

    private async Task ReplayOneAsync(QueuedRequest item, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        using var request = item.ToHttpRequestMessage();
        HttpResponseMessage? response = null;

        try
        {
            var client = ResolveClient(item.HttpClientName);
            response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode || !IsTransient(response.StatusCode, options))
            {
                await _queue.RemoveAsync(item.Id, cancellationToken).ConfigureAwait(false);
                options.Events.OnReplayed?.Invoke(item, response);
                _logger.LogInformation("Replayed queued request {RequestId} {Method} {Uri} -> {Status}.",
                    item.Id, item.Method, item.Uri, (int)response.StatusCode);
                return;
            }

            await HandleReplayFailureAsync(item, $"HTTP {(int)response.StatusCode}", options, response, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await HandleReplayFailureAsync(item, ex.Message, options, response, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(ex, "Replay failed for {RequestId} {Method} {Uri}.", item.Id, item.Method, item.Uri);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async Task HandleReplayFailureAsync(
        QueuedRequest item,
        string error,
        ApiResilienceOptions options,
        HttpResponseMessage? response,
        CancellationToken cancellationToken)
    {
        item.Attempts++;
        item.LastError = error;

        if (item.Attempts >= Math.Max(1, options.OfflineQueue.MaxReplayAttempts))
        {
            await _queue.DeadLetterAsync(item, cancellationToken).ConfigureAwait(false);
            options.Events.OnDeadLettered?.Invoke(item);
            options.Events.OnReplayed?.Invoke(item, response);
            _logger.LogError("Dead-lettered queued request {RequestId} after {Attempts} attempts: {Error}.",
                item.Id, item.Attempts, error);
            return;
        }

        await _queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        options.Events.OnReplayed?.Invoke(item, response);
    }

    private HttpClient ResolveClient(string? name)
    {
        if (_httpClientFactory is not null)
        {
            return _httpClientFactory.CreateClient(string.IsNullOrWhiteSpace(name)
                ? ApiResilienceDefaults.HttpClientName
                : name);
        }

        if (_fallbackClient is not null)
        {
            return _fallbackClient;
        }

        throw new InvalidOperationException(
            "No HttpClient is available to replay the offline queue. Register IHttpClientFactory or use ApiResilienceHttp.CreateClient.");
    }

    private static bool IsTransient(HttpStatusCode statusCode, ApiResilienceOptions options) =>
        options.Retry.TransientStatusCodes.Contains(statusCode);
}
