using Microsoft.Maui.Networking;
using Plugin.Maui.ApiResilience;

namespace Plugin.Maui.ApiResilience.Sample.Demo;

public sealed class DemoConnectivity : IConnectivityProvider, IDisposable
{
    public bool ForceOffline { get; private set; }

    public bool IsConnected =>
        !ForceOffline &&
        Connectivity.Current.NetworkAccess is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet;

    public event EventHandler<NetworkAccessChangedEventArgs>? ConnectivityChanged;

    public DemoConnectivity()
    {
        Connectivity.Current.ConnectivityChanged += OnMauiChanged;
    }

    public void SetForceOffline(bool offline)
    {
        ForceOffline = offline;
        ConnectivityChanged?.Invoke(this, new NetworkAccessChangedEventArgs(IsConnected));
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnMauiChanged;
    }

    private void OnMauiChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, new NetworkAccessChangedEventArgs(IsConnected));
    }
}
