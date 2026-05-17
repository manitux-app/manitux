using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using Manitux.Core.Application;

namespace Manitux.Services.Localizations
{
    public interface ILocalizationService
    {
        AppStrings Strings { get; set; }
        string CurrentCulture { get; }
        IReadOnlyList<CultureInfo> SupportedCultures { get; }
        event EventHandler? LanguageChanged;
        Task SelectSystemOrDefaultLanguageAsync();
        Task ChangeLanguageAsync(string cultureCode);
        void ChangeLanguage(string cultureCode);
    }

    public class LocalizationService : ILocalizationService
    {
        public AppStrings Strings { get; set; } = new AppStrings();
        public string CurrentCulture { get; private set; } = "en-US";
        public IReadOnlyList<CultureInfo> SupportedCultures { get; } =
        [
            new CultureInfo("tr-TR"),
            new CultureInfo("en-US"),
            new CultureInfo("de-DE"),
            new CultureInfo("fr-FR")
        ];

        public event EventHandler? LanguageChanged;

        public Task SelectSystemOrDefaultLanguageAsync()
        {
            var culture = ResolveCulture(CultureInfo.CurrentUICulture);
            return ChangeLanguageAsync(culture.Name);
        }

        public void ChangeLanguage(string cultureCode)
        {
            _ = ChangeLanguageAsync(cultureCode);
        }

        public async Task ChangeLanguageAsync(string cultureCode)
        {
            var culture = ResolveCulture(new CultureInfo(cultureCode));
            var appContext = CodeLogic.CodeLogic.GetApplicationContext();
            //Debug.WriteLine("ChangeLanguage AppContext: " + appContext?.ApplicationId);
           
            if (appContext != null)
            {
                await EnsureSupportedCulturesLoadedAsync();
                var loadedCulture = GetLoadedCultureOrDefault(culture.Name);
                var localized = appContext.Localization.Get<AppStrings>(loadedCulture);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CopyStrings(localized, Strings);
                    CurrentCulture = loadedCulture;
                    Debug.WriteLine("ChangeLanguage Culture: " + CurrentCulture);
                    Strings.RefreshBindings();
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }

        private async Task EnsureSupportedCulturesLoadedAsync()
        {
            var appContext = CodeLogic.CodeLogic.GetApplicationContext();
            if (appContext is null)
            {
                return;
            }

            var cultures = SupportedCultures.Select(x => x.Name).ToList();
            try
            {
                await appContext.Localization.LoadAsync<AppStrings>(cultures, generateIfMissing: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Localization reload failed: {ex}");
            }
        }

        private string GetLoadedCultureOrDefault(string cultureName)
        {
            var appContext = CodeLogic.CodeLogic.GetApplicationContext();
            if (appContext is null)
            {
                return "en-US";
            }

            var loadedCultures = appContext.Localization.GetLoadedCultures<AppStrings>();
            var requestedCulture = loadedCultures.FirstOrDefault(x =>
                string.Equals(x, cultureName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(requestedCulture))
            {
                return requestedCulture;
            }

            var englishCulture = loadedCultures.FirstOrDefault(x =>
                string.Equals(x, "en-US", StringComparison.OrdinalIgnoreCase));

            return englishCulture ?? "en-US";
        }

        private CultureInfo ResolveCulture(CultureInfo requestedCulture)
        {
            var exact = SupportedCultures.FirstOrDefault(x =>
                string.Equals(x.Name, requestedCulture.Name, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }

            var sameLanguage = SupportedCultures.FirstOrDefault(x =>
                string.Equals(x.TwoLetterISOLanguageName, requestedCulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));

            return sameLanguage ?? SupportedCultures.First(x => x.Name == "en-US");
        }

        private static void CopyStrings(AppStrings source, AppStrings target)
        {
            foreach (var property in typeof(AppStrings).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(x => x.CanRead && x.CanWrite))
            {
                property.SetValue(target, property.GetValue(source));
            }
        }
    }
}
