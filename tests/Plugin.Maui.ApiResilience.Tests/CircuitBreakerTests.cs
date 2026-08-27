using System.Net;
using Plugin.Maui.ApiResilience;
using Xunit;

namespace Plugin.Maui.ApiResilience.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task Opens_after_consecutive_failures_and_fails_fast()
    {
        var options = new ApiResilienceOptions();
        options.Retry.Enabled = false;
        options.Timeout.Enabled = false;
        options.OfflineQueue.Enabled = false;
        options.CircuitBreaker.Enabled = true;
        options.CircuitBreaker.MinimumThroughput = 2;
        options.CircuitBreaker.FailureRatio = 1;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(1);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);

        var opened = 0;
        options.Events.OnCircuitOpened = _ => opened++;

        var inner = new ScriptedHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var client = HandlerFactory.CreateClient(options, inner);

        var first = await client.GetAsync("/down");
        var second = await client.GetAsync("/down");
        Assert.Equal(HttpStatusCode.InternalServerError, first.StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, second.StatusCode);

        await Assert.ThrowsAsync<CircuitOpenException>(() => client.GetAsync("/down"));
        Assert.Equal(2, inner.Calls);
        Assert.Equal(1, opened);
    }
}
