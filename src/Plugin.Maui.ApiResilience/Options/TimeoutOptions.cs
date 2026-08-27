namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Overall timeout applied around retries and the inner HTTP call.
/// </summary>
public sealed class TimeoutOptions
{
    /// <summary>
    /// Enables the timeout strategy. Default is <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum time allowed for the full attempt sequence (including retries). Default is 30 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
