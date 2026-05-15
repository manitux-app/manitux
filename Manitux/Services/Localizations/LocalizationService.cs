using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Manitux.Core.Application;

namespace Manitux.Services.Localizations
{
    public interface ILocalizationService
    {
        AppStrings Strings { get; set; }
        void ChangeLanguage(string cultureCode);
    }

    public class LocalizationService : ILocalizationService
    {
        public AppStrings Strings { get; set; } = new AppStrings();

        public void ChangeLanguage(string cultureCode)
        {
            var appContext = CodeLogic.CodeLogic.GetApplicationContext();
            //Debug.WriteLine("ChangeLanguage AppContext: " + appContext?.ApplicationId);
           
            if (appContext != null)
            {
                Strings = appContext.Localization.Get<AppStrings>(cultureCode);
                Debug.WriteLine("ChangeLanguage Culture: " + cultureCode);
                Strings.RefreshBindings();
            }
        }
    }
}
