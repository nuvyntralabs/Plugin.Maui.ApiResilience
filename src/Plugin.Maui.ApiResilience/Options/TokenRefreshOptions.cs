namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Attaches a bearer token and refreshes it once when the API returns 401.
/// </summary>
public sealed class TokenRefreshOptions
{
    /// <summary>
    /// Enables token attachment and 401 refresh. Default is <see langword="false"/> until a provider is configured.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Authorization scheme written on the request. Default is <c>Bearer</c>.
    /// </summary>
    public string AuthenticationScheme { get; set; } = "Bearer";

    /// <summary>
    /// Status codes that trigger a refresh. Default is 401 Unauthorized.
    /// </summary>
    public ISet<HttpStatusCode> UnauthorizedStatusCodes { get; } = new HashSet<HttpStatusCode>
    {
        HttpStatusCode.Unauthorized
    };

    /// <summary>
    /// When <see langword="true"/>, a failed refresh throws <see cref="TokenRefreshException"/>.
    /// When <see langword="false"/>, the original unauthorized response is returned. Default is <see langword="false"/>.
    /// </summary>
    public bool RethrowOnRefreshFailure { get; set; }

    /// <summary>
    /// Optional callback used when <see cref="IAccessTokenProvider"/> is not registered.
    /// </summary>
    public Func<CancellationToken, Task<string?>>? GetAccessTokenAsync { get; set; }

    /// <summary>
    /// Optional callback used when <see cref="IAccessTokenProvider"/> is not registered.
    /// Should persist the new access token before returning it.
    /// </summary>
    public Func<CancellationToken, Task<string?>>? RefreshAccessTokenAsync { get; set; }
}
