namespace Plugin.Maui.ApiResilience;

/// <summary>
/// Resolves a writable app-data folder for the offline queue file.
/// </summary>
public interface IAppStorage
{
    /// <summary>
    /// Directory that survives app restarts and is private to the app.
    /// </summary>
    string AppDataDirectory { get; }
}
