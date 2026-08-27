using System.Net;
using Plugin.Maui.ApiResilience;
using Xunit;

namespace Plugin.Maui.ApiResilience.Tests;

public sealed class TokenRefreshTests
{
    [Fact]
    public async Task Attaches_bearer_token_and_refreshes_on_401()
    {
        var tokens = new FakeTokenProvider { AccessToken = "old", RefreshTokenValue = "new" };
        var options = HandlerFactory.FastRetry();
        options.TokenRefresh.Enabled = true;
        options.Retry.Enabled = false;

        var inner = new ScriptedHandler((request, call) =>
        {
            var token = request.Headers.Authorization?.Parameter;
            if (token == "old")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var client = HandlerFactory.CreateClient(options, inner, tokens: tokens);

        var response = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Calls);
        Assert.Equal(1, tokens.RefreshCalls);
        Assert.Equal("Bearer old", inner.AuthorizationHeaders[0]);
        Assert.Equal("Bearer new", inner.AuthorizationHeaders[1]);
    }

    [Fact]
    public async Task Skips_refresh_when_token_already_rotated()
    {
        var tokens = new FakeTokenProvider { AccessToken = "old", RefreshTokenValue = "new" };
        var options = HandlerFactory.FastRetry();
        options.TokenRefresh.Enabled = true;
        options.Retry.Enabled = false;

        var inner = new ScriptedHandler((_, call) =>
        {
            if (call == 1)
            {
                tokens.AccessToken = "new";
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        tokens.RefreshImpl = _ => throw new InvalidOperationException("refresh should not run");

        using var client = HandlerFactory.CreateClient(options, inner, tokens: tokens);

        var response = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, tokens.RefreshCalls);
    }
}
