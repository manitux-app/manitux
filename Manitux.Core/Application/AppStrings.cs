using System;
using System.ComponentModel;
using CodeLogic.Core.Localization;

namespace Manitux.Core.Application;

[LocalizationSection("manitux")]
public class AppStrings: LocalizationModelBase, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshBindings()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    // ── Startup / shutdown ────────────────────────────────────────────────
    public string Welcome     { get; set; } = "{0} started.";
    public string Goodbye     { get; set; } = "{0} stopped.";
    public string Starting    { get; set; } = "Starting up...";
    public string Stopping    { get; set; } = "Shutting down...";

    // ── Status messages ───────────────────────────────────────────────────
    public string Ready       { get; set; } = "Ready. Press Q to quit.";
    public string Processing  { get; set; } = "Processing {0} items...";
    public string Completed   { get; set; } = "Completed in {0}ms";
    public string Failed      { get; set; } = "Failed: {0}";

    // ── Health ────────────────────────────────────────────────────────────
    public string Healthy     { get; set; } = "All systems healthy";
    public string Unhealthy   { get; set; } = "Degraded: {0}";

    // ── GUI ────────────────────────────────────────────────────────────
    //[LocalizedString]
    public string AboutUs { get; set; } = "About Us";
    public string Settings { get; set; } = "Settings";
    public string Plugins { get; set; } = "Plugins";
    public string Favorites { get; set; } = "Favorites";
    public string Search { get; set; } = "Search";
    public string Refresh { get; set; } = "Refresh";
    public string Country { get; set; } = "Country";
    public string Duration { get; set; } = "Duration";
    public string Year { get; set; } = "Year";
    public string Season { get; set; } = "Season";
    public string Episode { get; set; } = "Episode";
    public string Close { get; set; } = "Close";
    public string Closed { get; set; } = "Closed";
    public string Theme { get; set; } = "Theme";
    public string Imdb { get; set; } = "IMDB";
    public string ManituxPlayer { get; set; } = "Manitux Player";
    public string VlcPlayer { get; set; } = "VLC Player";
    public string MpvPlayer { get; set; } = "MPV Player";
    public string AddToFavorites { get; set; } = "Add to favorites";
    public string RemoveFromFavorites { get; set; } = "Remove from favorites";
    public string PluginConfigTitleFormat { get; set; } = "{0} config.json";
    public string MainUrl { get; set; } = "Main URL";
    public string ApiKey { get; set; } = "API Key";
    public string Favicon { get; set; } = "Favicon";
    public string Language { get; set; } = "Language";
    public string UseProxy { get; set; } = "Use proxy";
    public string AdultContent { get; set; } = "Adult content";
    public string Cancel { get; set; } = "Cancel";
    public string Save { get; set; } = "Save";
    public string Add { get; set; } = "Add";
    public string Open { get; set; } = "Open";
    public string Install { get; set; } = "Install";
    public string Update { get; set; } = "Update";
    public string UpdateAll { get; set; } = "Update all";
    public string Remove { get; set; } = "Remove";
    public string Repositories { get; set; } = "Repositories";
    public string AvailablePlugins { get; set; } = "Available Plugins";
    public string InstalledPlugins { get; set; } = "Installed Plugins";
    public string RepositoryInputWatermark { get; set; } = "GitHub repo URL, raw repo.json, plugins.json, plugin URL, or short code";

    // ── GUI Messages ────────────────────────────────────────────────────────────
    public string AppNotInitialized { get; set; } = "ManituxApp is not initialized!";
    public string PlayerNotInitialized { get; set; } = "Player is not initialized!";
    public string VideoNotInitialized { get; set; } = "Video is not initialized!";
    public string PageNotFound { get; set; } = "Page not found!";
    public string Error { get; set; } = "Error";
    public string PluginNotSelected { get; set; } = "Plugin not selected";
    public string NoFavoritesFound { get; set; } = "No favorites found";
    public string AddedToFavorites { get; set; } = "Added to favorites";
    public string RemovedFromFavorites { get; set; } = "Removed from favorites";
    public string NoPluginsLoadedTitle { get; set; } = "No plugins loaded yet!";
    public string NoPluginsLoadedMessage { get; set; } = "Manitux does not have any plugins installed. Users install plugins themselves.";
    public string ManituxDesktopApp { get; set; } = "Manitux Desktop App";
    public string ManituxMobileApp { get; set; } = "Manitux Mobile App";
    public string RepositoryRequired { get; set; } = "Repository URL or short code is required.";
    public string RepositoryAddedFormat { get; set; } = "Repository added: {0}";
    public string RepositoryLoadedFormat { get; set; } = "Repository loaded: {0}";
    public string PluginRemovedFormat { get; set; } = "Plugin removed: {0}";
    public string PluginWasNotFound { get; set; } = "Plugin was not found.";
    public string UpdateCheckCompletedFormat { get; set; } = "Update check completed. {0}/{1} plugins processed.";
    
}
