using System.Net;
using System.Text;

namespace Plugin.Maui.ApiResilience.Sample.Demo;

/// <summary>
/// In-process API used by the sample so retry, circuit breaker, 401 refresh, and queued POSTs
/// can be exercised without a public backend.
/// </summary>
public sealed class DemoApiHandler : HttpMessageHandler
{
    private int _retryHits;
    private int _circuitHits;
    private int _secureHits;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath.Trim('/').ToLowerInvariant() ?? string.Empty;

        HttpResponseMessage response = path switch
        {
            "retry" => HandleRetry(),
            "circuit" => HandleCircuit(),
            "secure" => HandleSecure(request),
            "queue" => Json(HttpStatusCode.Created, $"{{\"queuedReplay\":true,\"method\":\"{request.Method}\"}}"),
            _ => Json(HttpStatusCode.NotFound, "{\"error\":\"unknown demo route\"}")
        };

        return Task.FromResult(response);
    }

    private HttpResponseMessage HandleRetry()
    {
        var hit = Interlocked.Increment(ref _retryHits);
        if (hit % 3 != 0)
        {
            return Json(HttpStatusCode.ServiceUnavailable, $"{{\"attempt\":{hit},\"message\":\"transient\"}}");
        }

        return Json(HttpStatusCode.OK, $"{{\"attempt\":{hit},\"message\":\"recovered after retry\"}}");
    }

    private HttpResponseMessage HandleCircuit()
    {
        var hit = Interlocked.Increment(ref _circuitHits);
        return Json(HttpStatusCode.InternalServerError, $"{{\"attempt\":{hit},\"message\":\"downstream down\"}}");
    }

    private HttpResponseMessage HandleSecure(HttpRequestMessage request)
    {
        Interlocked.Increment(ref _secureHits);
        var token = request.Headers.Authorization?.Parameter ?? string.Empty;
        if (token.StartsWith("expired", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(token))
        {
            return Json(HttpStatusCode.Unauthorized, "{\"error\":\"token expired\"}");
        }

        return Json(HttpStatusCode.OK, $"{{\"message\":\"hello\",\"token\":\"{token}\"}}");
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
