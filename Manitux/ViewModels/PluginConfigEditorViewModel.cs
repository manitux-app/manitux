using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using Manitux.Core.Application;
using Manitux.Core.Plugins;
using Manitux.Services.Localizations;

namespace Manitux.ViewModels;

public partial class PluginConfigEditorViewModel : ViewModelBase, IDialogContext
{
    private readonly PluginBase _plugin;
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _mainUrl = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _favicon = string.Empty;
    [ObservableProperty] private string _language = string.Empty;
    [ObservableProperty] private bool _useProxy;
    [ObservableProperty] private bool _isAdult;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasError;

    public AppStrings L { get; }

    public PluginConfigEditorViewModel(PluginBase plugin, ILocalizationService localizationService)
    {
        _plugin = plugin;
        L = localizationService.Strings;
        Title = string.Format(L.PluginConfigTitleFormat, plugin.Manifest.Name);
        _configPath = GetPluginConfigPath(plugin.Manifest.Id);
        LoadConfig();
    }

    public event EventHandler<object?>? RequestClose;

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            var config = CreateConfigFromForm();

            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            var formattedJson = JsonSerializer.Serialize(config, _jsonOptions);
            await File.WriteAllTextAsync(_configPath, formattedJson);

            _plugin.Config = config;
            HasError = false;
            ErrorMessage = null;
            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var config = JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(_configPath), _jsonOptions)
                             ?? _plugin.Config;
                ApplyConfig(config);
                return;
            }

            ApplyConfig(_plugin.Config);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            ApplyConfig(_plugin.Config);
        }
    }

    private void ApplyConfig(PluginConfig config)
    {
        MainUrl = config.MainUrl;
        ApiKey = config.ApiKey;
        Favicon = config.Favicon;
        Language = config.Language;
        UseProxy = config.UseProxy;
        IsAdult = config.IsAdult;
    }

    private PluginConfig CreateConfigFromForm()
    {
        return new PluginConfig
        {
            MainUrl = MainUrl.Trim(),
            ApiKey = ApiKey.Trim(),
            Favicon = Favicon.Trim(),
            Language = Language.Trim(),
            UseProxy = UseProxy,
            IsAdult = IsAdult
        };
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private static string GetPluginConfigPath(string pluginId)
    {
        var baseDir = OperatingSystem.IsAndroid()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : AppContext.BaseDirectory;

        return Path.Combine(baseDir, "data", "plugins", pluginId, "config.json");
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }
}
