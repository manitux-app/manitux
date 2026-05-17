using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manitux.Services.Localizations;

namespace Manitux.ViewModels;

public partial class LocaleViewModel : ViewModelBase
{
    private readonly ILocalizationService _localizationService;

    public ObservableCollection<LocaleItemViewModel> MenuItems { get; set; }

    [ObservableProperty]
    private string? _selectedCultureName;

    public LocaleViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        MenuItems = new ObservableCollection<LocaleItemViewModel>(
            _localizationService.SupportedCultures.Select(CreateLocaleItem));

        SelectedCultureName = _localizationService.CurrentCulture;
        _localizationService.LanguageChanged += OnLanguageChanged;
        UpdateSelectedLocale();
        OnPropertyChanged(nameof(MenuItems));
    }

    [RelayCommand]
    private async Task SelectLocale(object? obj)
    {
        if (obj is CultureInfo culture)
        {
            await _localizationService.ChangeLanguageAsync(culture.Name);
        }
    }

    private LocaleItemViewModel CreateLocaleItem(CultureInfo culture)
    {
        return new LocaleItemViewModel
        {
            Header = culture.NativeName,
            CultureName = culture.Name,
            Command = SelectLocaleCommand,
            CommandParameter = culture
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        SelectedCultureName = _localizationService.CurrentCulture;
        UpdateSelectedLocale();
    }

    private void UpdateSelectedLocale()
    {
        foreach (var item in MenuItems)
        {
            item.IsSelected = string.Equals(item.CultureName, SelectedCultureName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public partial class LocaleItemViewModel : ViewModelBase
{
    public string? Header { get; set; }
    public string? CultureName { get; set; }
    public string DisplayHeader => IsSelected ? $"✓ {Header}" : Header ?? string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayHeader));
    }

    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }
    public IList<LocaleItemViewModel>? Items { get; set; }
}
