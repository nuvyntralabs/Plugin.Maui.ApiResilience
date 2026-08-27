namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Thrown when access-token refresh fails and <see cref="TokenRefreshOptions.RethrowOnRefreshFailure"/> is enabled.
/// </summary>
public sealed class TokenRefreshException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    public TokenRefreshException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
