using System;
using CodeLogic.Core.Localization;

namespace Manitux.Core.Application;

[LocalizationSection("manitux")]
public class AppStrings: LocalizationModelBase
{
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

    // ── GUI Menu────────────────────────────────────────────────────────────
    //[LocalizedString]
    public string AboutUs { get; set; } = "About Us";
    public string Settings { get; set; } = "Settings";
    public string Plugins { get; set; } = "Plugins";

    // ── GUI Messages ────────────────────────────────────────────────────────────
    public string NotInitialized { get; set; } = "ManituxApp is not initialized!";
    public string NotPageFound { get; set; } = "Not page found: ";
    
}
