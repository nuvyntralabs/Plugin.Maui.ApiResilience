namespace Plugin.Maui.ApiResilience;

internal sealed class DelegateAccessTokenProvider : IAccessTokenProvider
{
    private readonly TokenRefreshOptions _options;

    public DelegateAccessTokenProvider(TokenRefreshOptions options)
    {
        _options = options;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return _options.GetAccessTokenAsync is null
            ? Task.FromResult<string?>(null)
            : _options.GetAccessTokenAsync(cancellationToken);
    }

    public Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return _options.RefreshAccessTokenAsync is null
            ? Task.FromResult<string?>(null)
            : _options.RefreshAccessTokenAsync(cancellationToken);
    }
}
