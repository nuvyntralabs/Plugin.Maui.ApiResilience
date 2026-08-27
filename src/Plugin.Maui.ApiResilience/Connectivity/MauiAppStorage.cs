using Microsoft.Maui.Storage;

namespace Plugin.Maui.ApiResilience;

/// <summary>
/// <see cref="IAppStorage"/> backed by MAUI <see cref="FileSystem.AppDataDirectory"/>.
/// </summary>
public sealed class MauiAppStorage : IAppStorage
{
    /// <inheritdoc />
    public string AppDataDirectory => FileSystem.AppDataDirectory;
}
