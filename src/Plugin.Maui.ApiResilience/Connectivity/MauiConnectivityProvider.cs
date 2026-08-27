using Microsoft.Maui.Networking;

namespace Plugin.Maui.ApiResilience;

/// <summary>
/// <see cref="IConnectivityProvider"/> backed by MAUI <see cref="IConnectivity"/>.
/// </summary>
public sealed class MauiConnectivityProvider : IConnectivityProvider, IDisposable
{
    private readonly IConnectivity _connectivity;

    /// <summary>
    /// Uses <see cref="Connectivity.Current"/>.
    /// </summary>
    public MauiConnectivityProvider()
        : this(Microsoft.Maui.Networking.Connectivity.Current)
    {
    }

    /// <summary>
    /// Uses the supplied MAUI connectivity service.
    /// </summary>
    public MauiConnectivityProvider(IConnectivity connectivity)
    {
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    /// <inheritdoc />
    public bool IsConnected =>
        _connectivity.NetworkAccess is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet;

    /// <inheritdoc />
    public event EventHandler<NetworkAccessChangedEventArgs>? ConnectivityChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, new NetworkAccessChangedEventArgs(IsConnected));
    }
}
