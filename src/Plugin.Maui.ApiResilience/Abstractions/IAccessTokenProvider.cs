namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Supplies and refreshes the access token attached to outbound HTTP calls.
/// Register this in DI, or set the delegates on <see cref="TokenRefreshOptions"/>.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Returns the current access token, or <see langword="null"/> if the user is anonymous.
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges the refresh token (or equivalent) for a new access token.
    /// Implementations should persist the new token before returning.
    /// </summary>
    Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default);
}
