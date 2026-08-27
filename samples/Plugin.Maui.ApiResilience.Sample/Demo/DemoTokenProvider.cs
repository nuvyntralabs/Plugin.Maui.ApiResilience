using Plugin.Maui.ApiResilience;

namespace Plugin.Maui.ApiResilience.Sample.Demo;

public sealed class DemoTokenProvider : IAccessTokenProvider
{
    public string AccessToken { get; private set; } = "expired-token";

    public int RefreshCount { get; private set; }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(AccessToken);

    public Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        RefreshCount++;
        AccessToken = $"access-{RefreshCount}";
        return Task.FromResult<string?>(AccessToken);
    }

    public void Expire() => AccessToken = "expired-token";
}
