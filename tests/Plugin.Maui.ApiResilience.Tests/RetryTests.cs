using System.Net;
using System.Net.Http;
using Plugin.Maui.ApiResilience;
using Xunit;

namespace Plugin.Maui.ApiResilience.Tests;

public sealed class RetryTests
{
    [Fact]
    public async Task Retries_transient_status_then_succeeds()
    {
        var inner = new ScriptedHandler((_, call) =>
            call < 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });

        using var client = HandlerFactory.CreateClient(HandlerFactory.FastRetry(), inner);

        var response = await client.GetAsync("/unstable");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, inner.Calls);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Resends_request_body_on_retry()
    {
        var inner = new ScriptedHandler((_, call) =>
            call == 1
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                : new HttpResponseMessage(HttpStatusCode.Created));

        using var client = HandlerFactory.CreateClient(HandlerFactory.FastRetry(), inner);

        var response = await client.PostAsync("/orders", new StringContent("{\"id\":1}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, inner.Calls);
        Assert.All(inner.Bodies, body => Assert.Equal("{\"id\":1}", body));
    }

    [Fact]
    public async Task Does_not_retry_client_errors()
    {
        var inner = new ScriptedHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest));
        using var client = HandlerFactory.CreateClient(HandlerFactory.FastRetry(), inner);

        var response = await client.GetAsync("/bad");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Raises_OnRetry()
    {
        var retries = new List<int>();
        var options = HandlerFactory.FastRetry();
        options.Events.OnRetry = e => retries.Add(e.AttemptNumber);

        var inner = new ScriptedHandler((_, call) =>
            call == 1
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK));

        using var client = HandlerFactory.CreateClient(options, inner);
        await client.GetAsync("/flaky");

        Assert.Single(retries);
    }
}
