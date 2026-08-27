namespace Plugin.Maui.ApiResilience;

/// <summary>
/// JSON file store for queued HTTP requests, written under the app data directory.
/// </summary>
public sealed class FileOfflineRequestQueue : IOfflineRequestQueue, IDisposable
{
    private readonly IAppStorage _storage;
    private readonly IOptionsMonitor<ApiResilienceOptions> _options;
    private readonly ILogger<FileOfflineRequestQueue> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;

    /// <summary>
    /// Creates the queue.
    /// </summary>
    public FileOfflineRequestQueue(
        IAppStorage storage,
        IOptionsMonitor<ApiResilienceOptions> options,
        ILogger<FileOfflineRequestQueue>? logger = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<FileOfflineRequestQueue>.Instance;
        Directory.CreateDirectory(_storage.AppDataDirectory);
        _path = Path.Combine(_storage.AppDataDirectory, ApiResilienceDefaults.QueueFileName);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var state = await ReadAsync(cancellationToken).ConfigureAwait(false);
        return state.Pending.Count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QueuedRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var state = await ReadAsync(cancellationToken).ConfigureAwait(false);
        return state.Pending.ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QueuedRequest>> GetDeadLettersAsync(CancellationToken cancellationToken = default)
    {
        var state = await ReadAsync(cancellationToken).ConfigureAwait(false);
        return state.DeadLetter.ToArray();
    }

    /// <inheritdoc />
    public async Task<string> EnqueueAsync(QueuedRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            request.Id = Guid.NewGuid().ToString("N");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var max = Math.Max(1, _options.CurrentValue.OfflineQueue.MaxQueueSize);

            if (state.Pending.Count >= max)
            {
                if (_options.CurrentValue.OfflineQueue.OverflowBehavior == OfflineQueueOverflowBehavior.RejectNew)
                {
                    throw new InvalidOperationException(
                        $"Offline queue is full ({max} requests). The request was not stored.");
                }

                var dropped = state.Pending[0];
                state.Pending.RemoveAt(0);
                _logger.LogWarning("Offline queue overflow. Dropped oldest request {RequestId} {Method} {Uri}.",
                    dropped.Id, dropped.Method, dropped.Uri);
            }

            state.Pending.Add(request);
            await SaveUnlockedAsync(state, cancellationToken).ConfigureAwait(false);
            return request.Id;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(QueuedRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return MutateAsync(state =>
        {
            var index = state.Pending.FindIndex(x => x.Id == request.Id);
            if (index >= 0)
            {
                state.Pending[index] = request;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return MutateAsync(state => state.Pending.RemoveAll(x => x.Id == id), cancellationToken);
    }

    /// <inheritdoc />
    public Task DeadLetterAsync(QueuedRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return MutateAsync(state =>
        {
            state.Pending.RemoveAll(x => x.Id == request.Id);
            if (state.DeadLetter.All(x => x.Id != request.Id))
            {
                state.DeadLetter.Add(request);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveUnlockedAsync(new QueueState(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task MutateAsync(Action<QueueState> mutate, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
            mutate(state);
            await SaveUnlockedAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<QueueState> ReadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<QueueState> LoadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return new QueueState();
        }

        try
        {
            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var state = await JsonSerializer.DeserializeAsync(stream, QueueJsonContext.Default.QueueState, cancellationToken)
                .ConfigureAwait(false);
            return state ?? new QueueState();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _logger.LogError(ex, "Failed to read offline queue file {Path}. Starting with an empty queue.", _path);
            return new QueueState();
        }
    }

    private async Task SaveUnlockedAsync(QueueState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = _path + ".tmp";
        await using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, state, QueueJsonContext.Default.QueueState, cancellationToken)
                .ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Copy(temp, _path, overwrite: true);
        File.Delete(temp);
    }
}
