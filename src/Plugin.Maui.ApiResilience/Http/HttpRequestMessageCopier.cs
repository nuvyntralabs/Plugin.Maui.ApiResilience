namespace Plugin.Maui.ApiResilience;

internal static class HttpRequestMessageCopier
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host"
    };

    internal static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key"
    };

    public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            clone.Content = new ByteArrayContent(bytes);
            CopyHeaders(request.Content.Headers, clone.Content.Headers, skipSensitive: false);
        }

        CopyHeaders(request.Headers, clone.Headers, skipSensitive: false);

        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }

    public static async Task<QueuedRequest> ToQueuedRequestAsync(
        this HttpRequestMessage request,
        string? httpClientName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        byte[]? content = null;
        string? contentType = null;
        if (request.Content is not null)
        {
            content = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            contentType = request.Content.Headers.ContentType?.ToString();
        }

        var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key) || SensitiveHeaders.Contains(header.Key))
            {
                continue;
            }

            headers[header.Key] = header.Value.ToArray();
        }

        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                headers[header.Key] = header.Value.ToArray();
            }
        }

        return new QueuedRequest
        {
            Id = Guid.NewGuid().ToString("N"),
            Method = request.Method.Method,
            Uri = request.RequestUri?.ToString() ?? string.Empty,
            Headers = headers,
            ContentBase64 = content is { Length: > 0 } ? Convert.ToBase64String(content) : null,
            ContentType = contentType,
            EnqueuedAtUtc = DateTimeOffset.UtcNow,
            Attempts = 0,
            HttpClientName = httpClientName
        };
    }

    public static HttpRequestMessage ToHttpRequestMessage(this QueuedRequest queued)
    {
        ArgumentNullException.ThrowIfNull(queued);

        var request = new HttpRequestMessage(new HttpMethod(queued.Method), queued.Uri);
        request.Options.Set(ApiResilienceDefaults.IsReplayKey, true);
        if (!string.IsNullOrWhiteSpace(queued.HttpClientName))
        {
            request.Options.Set(ApiResilienceDefaults.HttpClientNameKey, queued.HttpClientName);
        }

        if (queued.ContentBase64 is not null)
        {
            var bytes = Convert.FromBase64String(queued.ContentBase64);
            request.Content = new ByteArrayContent(bytes);
            if (!string.IsNullOrWhiteSpace(queued.ContentType))
            {
                request.Content.Headers.TryAddWithoutValidation("Content-Type", queued.ContentType);
            }
        }

        foreach (var header in queued.Headers)
        {
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value) && request.Content is not null)
            {
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return request;
    }

    public static bool IsReplay(this HttpRequestMessage request)
    {
        return request.Options.TryGetValue(ApiResilienceDefaults.IsReplayKey, out var replay) && replay;
    }

    private static void CopyHeaders(HttpHeaders source, HttpHeaders destination, bool skipSensitive)
    {
        foreach (var header in source)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            if (skipSensitive && SensitiveHeaders.Contains(header.Key))
            {
                continue;
            }

            destination.TryAddWithoutValidation(header.Key, header.Value);
        }
    }
}
