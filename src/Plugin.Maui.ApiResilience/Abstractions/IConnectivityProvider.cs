namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Reports device network availability. The default implementation uses MAUI <c>Connectivity</c>.
/// </summary>
public interface IConnectivityProvider
{
    /// <summary>
    /// <see langword="true"/> when the device has internet (including constrained internet).
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Raised when <see cref="IsConnected"/> may have changed.
    /// </summary>
    event EventHandler<NetworkAccessChangedEventArgs>? ConnectivityChanged;
}

/// <summary>
/// Connectivity change payload.
/// </summary>
public sealed class NetworkAccessChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates the args.
    /// </summary>
    public NetworkAccessChangedEventArgs(bool isConnected)
    {
        IsConnected = isConnected;
    }

    /// <summary>
    /// Current connectivity.
    /// </summary>
    public bool IsConnected { get; }
}
