using System.Net;
using Plugin.Maui.ApiResilience;
using Xunit;

namespace Plugin.Maui.ApiResilience.Tests;

public sealed class OfflineQueueTests
{
    [Fact]
    public async Task Queues_mutating_request_when_disconnected()
    {
        var connectivity = new FakeConnectivity { IsConnected = false };
        var queue = new InMemoryOfflineRequestQueue();
        var options = HandlerFactory.FastRetry();
        options.OfflineQueue.Enabled = true;
        options.OfflineQueue.ResponseMode = OfflineQueueResponseMode.ThrowException;

        var inner = new ScriptedHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = HandlerFactory.CreateClient(options, inner, connectivity, queue);

        var ex = await Assert.ThrowsAsync<RequestQueuedOfflineException>(
            () => client.PostAsync("/orders", new StringContent("payload")));

        Assert.Equal(0, inner.Calls);
        Assert.Equal(1, await queue.CountAsync());
        Assert.False(string.IsNullOrWhiteSpace(ex.RequestId));
        var pending = await queue.GetPendingAsync();
        Assert.Equal("POST", pending[0].Method);
        Assert.Contains("payload", Convert.FromBase64String(pending[0].ContentBase64!).Length > 0
            ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(pending[0].ContentBase64!))
            : string.Empty);
        Assert.False(pending[0].Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task Returns_accepted_when_configured()
    {
        var connectivity = new FakeConnectivity { IsConnected = false };
        var queue = new InMemoryOfflineRequestQueue();
        var options = HandlerFactory.FastRetry();
        options.OfflineQueue.Enabled = true;
        options.OfflineQueue.ResponseMode = OfflineQueueResponseMode.AcceptedResponse;

        var inner = new ScriptedHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = HandlerFactory.CreateClient(options, inner, connectivity, queue);

        var response = await client.PutAsync("/profile", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.True(response.Headers.Contains("X-ApiResilience-Queued"));
        Assert.Equal(1, await queue.CountAsync());
    }

    [Fact]
    public async Task Does_not_queue_get()
    {
        var connectivity = new FakeConnectivity { IsConnected = false };
        var options = HandlerFactory.FastRetry();
        options.OfflineQueue.Enabled = true;

        var inner = new ScriptedHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = HandlerFactory.CreateClient(options, inner, connectivity);

        var response = await client.GetAsync("/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Processor_replays_and_removes_successful_items()
    {
        var queue = new InMemoryOfflineRequestQueue();
        await queue.EnqueueAsync(new QueuedRequest
        {
            Id = "1",
            Method = "POST",
            Uri = "https://api.test.local/orders",
            ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"n\":1}")),
            ContentType = "application/json",
            EnqueuedAtUtc = DateTimeOffset.UtcNow
        });

        var inner = new ScriptedHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Created));
        using var replayClient = new HttpClient(inner) { BaseAddress = new Uri("https://api.test.local") };

        var options = new ApiResilienceOptions();
        var processor = new OfflineQueueProcessor(
            queue,
            new StaticOptionsMonitor<ApiResilienceOptions>(options),
            httpClientFactory: null,
            fallbackClient: replayClient);

        await processor.ProcessAsync();

        Assert.Equal(1, inner.Calls);
        Assert.Equal(0, await queue.CountAsync());
    }

    [Fact]
    public async Task File_queue_roundtrips_requests()
    {
        var dir = Path.Combine(Path.GetTempPath(), "apiresilience-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var options = new ApiResilienceOptions();
            using var fileQueue = new FileOfflineRequestQueue(
                new TempStorage(dir),
                new StaticOptionsMonitor<ApiResilienceOptions>(options));

            var id = await fileQueue.EnqueueAsync(new QueuedRequest
            {
                Method = "DELETE",
                Uri = "https://api.test.local/orders/9",
                EnqueuedAtUtc = DateTimeOffset.UtcNow
            });

            var pending = await fileQueue.GetPendingAsync();
            Assert.Single(pending);
            Assert.Equal(id, pending[0].Id);
            Assert.Equal("DELETE", pending[0].Method);

            await fileQueue.RemoveAsync(id);
            Assert.Equal(0, await fileQueue.CountAsync());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class TempStorage : IAppStorage
    {
        public TempStorage(string path) => AppDataDirectory = path;

        public string AppDataDirectory { get; }
    }
}
