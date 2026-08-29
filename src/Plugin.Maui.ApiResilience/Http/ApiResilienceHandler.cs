namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Delegating handler that applies token refresh, offline queuing, retry, circuit breaking, and timeout.
/// </summary>
public sealed class ApiResilienceHandler : DelegatingHandler
{
    private readonly ApiResilienceOptions _options;
    private readonly IConnectivityProvider _connectivity;
    private readonly IOfflineRequestQueue _queue;
    private readonly IAccessTokenProvider? _tokenProvider;
    private readonly ILogger<ApiResilienceHandler> _logger;
    private readonly ResiliencePipelineFactory _pipelines = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly string _httpClientName;
    private Task<string?>? _inFlightRefresh;

    /// <summary>
    /// Creates the handler used by DI.
    /// </summary>
    public ApiResilienceHandler(
        IOptionsMonitor<ApiResilienceOptions> options,
        IConnectivityProvider connectivity,
        IOfflineRequestQueue queue,
        IAccessTokenProvider? tokenProvider = null,
        ILogger<ApiResilienceHandler>? logger = null)
        : this(
            options.CurrentValue,
            connectivity,
            queue,
            tokenProvider,
            logger,
            ApiResilienceDefaults.HttpClientName)
    {
    }

    internal ApiResilienceHandler(
        ApiResilienceOptions options,
        IConnectivityProvider connectivity,
        IOfflineRequestQueue queue,
        IAccessTokenProvider? tokenProvider,
        ILogger<ApiResilienceHandler>? logger,
        string httpClientName)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _tokenProvider = tokenProvider ?? (options.TokenRefresh.Enabled
            ? new DelegateAccessTokenProvider(options.TokenRefresh)
            : null);
        _logger = logger ?? NullLogger<ApiResilienceHandler>.Instance;
        _httpClientName = httpClientName;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Options.Set(ApiResilienceDefaults.HttpClientNameKey, _httpClientName);

        var tokenBefore = await AttachTokenAsync(request, cancellationToken).ConfigureAwait(false);

        if (ShouldQueueBeforeSend(request))
        {
            return await EnqueueAsync(request, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var response = await SendWithResilienceAsync(request, cancellationToken).ConfigureAwait(false);

            if (ShouldRefreshToken(response) && !request.IsReplay())
            {
                var retried = await RefreshAndRetryAsync(request, tokenBefore, cancellationToken).ConfigureAwait(false);
                if (retried is not null)
                {
                    response.Dispose();
                    return retried;
                }
            }

            return response;
        }
        catch (BrokenCircuitException ex)
        {
            var scope = ResiliencePipelineFactory.GetScopeKey(_options, request);
            throw new CircuitOpenException(scope, _options.CircuitBreaker.BreakDuration, ex);
        }
        catch (Exception ex) when (ShouldQueueAfterFailure(request, ex))
        {
            _logger.LogWarning(ex, "Transport failure for {Method} {Uri}. Queuing offline.", request.Method, request.RequestUri);
            return await EnqueueAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<HttpResponseMessage> SendWithResilienceAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var scopeKey = ResiliencePipelineFactory.GetScopeKey(_options, request);
        var pipeline = _pipelines.GetPipeline(_options, scopeKey);

        return await pipeline.ExecuteAsync(async ct =>
        {
            using var attempt = await request.CloneAsync(ct).ConfigureAwait(false);
            return await base.SendAsync(attempt, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private bool ShouldQueueBeforeSend(HttpRequestMessage request)
    {
        return _options.OfflineQueue.Enabled
               && !request.IsReplay()
               && !_connectivity.IsConnected
               && IsQueueableMethod(request.Method);
    }

    private bool ShouldQueueAfterFailure(HttpRequestMessage request, Exception exception)
    {
        return _options.OfflineQueue.Enabled
               && _options.OfflineQueue.QueueOnTransportFailure
               && !request.IsReplay()
               && IsQueueableMethod(request.Method)
               && exception is not CircuitOpenException
               && exception is not BrokenCircuitException
               && exception is not OperationCanceledException { CancellationToken.IsCancellationRequested: true }
               && ResiliencePipelineFactory.IsTransientException(exception);
    }

    private bool IsQueueableMethod(HttpMethod method) =>
        _options.OfflineQueue.QueueableMethods.Any(m => m.Method.Equals(method.Method, StringComparison.OrdinalIgnoreCase));

    private async Task<HttpResponseMessage> EnqueueAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var queued = await request.ToQueuedRequestAsync(_httpClientName, cancellationToken).ConfigureAwait(false);
        await _queue.EnqueueAsync(queued, cancellationToken).ConfigureAwait(false);
        _options.Events.OnQueued?.Invoke(queued);
        _logger.LogInformation("Queued {Method} {Uri} as {RequestId}.", queued.Method, queued.Uri, queued.Id);

        if (_options.OfflineQueue.ResponseMode == OfflineQueueResponseMode.AcceptedResponse)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                ReasonPhrase = "Queued Offline",
                RequestMessage = request,
                Content = new StringContent($"{{\"queued\":true,\"id\":\"{queued.Id}\"}}")
            };
            response.Headers.TryAddWithoutValidation("X-ApiResilience-Queued", "true");
            response.Headers.TryAddWithoutValidation("X-ApiResilience-Request-Id", queued.Id);
            return response;
        }

        throw new RequestQueuedOfflineException(queued.Id, request.RequestUri, request.Method);
    }

    private bool ShouldRefreshToken(HttpResponseMessage response) =>
        _options.TokenRefresh.Enabled
        && _tokenProvider is not null
        && _options.TokenRefresh.UnauthorizedStatusCodes.Contains(response.StatusCode);

    private async Task<string?> AttachTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_options.TokenRefresh.Enabled || _tokenProvider is null)
        {
            return request.Headers.Authorization?.Parameter;
        }

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        ApplyToken(request, token);
        return token;
    }

    private void ApplyToken(HttpRequestMessage request, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = null;
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(_options.TokenRefresh.AuthenticationScheme, token);
    }

    private async Task<HttpResponseMessage?> RefreshAndRetryAsync(
        HttpRequestMessage request,
        string? tokenBefore,
        CancellationToken cancellationToken)
    {
        try
        {
            var latest = _tokenProvider is null
                ? null
                : await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

            if (string.Equals(latest, tokenBefore, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(latest))
            {
                latest = await RefreshOnceAsync(cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(latest))
            {
                return null;
            }

            ApplyToken(request, latest);
            return await SendWithResilienceAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not CircuitOpenException and not BrokenCircuitException and not OperationCanceledException)
        {
            _options.Events.OnTokenRefreshFailed?.Invoke(ex);
            _logger.LogError(ex, "Access token refresh failed.");

            if (_options.TokenRefresh.RethrowOnRefreshFailure)
            {
                throw new TokenRefreshException("Access token refresh failed.", ex);
            }

            return null;
        }
    }

    private async Task<string?> RefreshOnceAsync(CancellationToken cancellationToken)
    {
        if (_tokenProvider is null)
        {
            return null;
        }

        Task<string?> pending;
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_inFlightRefresh is { IsCompleted: false })
            {
                pending = _inFlightRefresh;
            }
            else
            {
                pending = _inFlightRefresh = RefreshSharedAsync();
            }
        }
        finally
        {
            _refreshGate.Release();
        }

        return await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> RefreshSharedAsync()
    {
        var token = await _tokenProvider!.RefreshAccessTokenAsync(CancellationToken.None).ConfigureAwait(false);
        _options.Events.OnTokenRefreshed?.Invoke();
        _logger.LogInformation("Access token refreshed.");
        return token;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshGate.Dispose();
        }

        base.Dispose(disposing);
    }
}
