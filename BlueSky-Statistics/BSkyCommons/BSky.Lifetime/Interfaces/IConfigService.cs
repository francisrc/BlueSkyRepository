using System.Collections.Generic;
using System.Collections.Specialized;

namespace BSky.Lifetime.Interfaces
{
    public interface IConfigService
    {

        #region Properties
        NameValueCollection AppSettings
        {
            get;
        }

        Dictionary<string, string> DefaultSettings
        {
            get;
        }

        string Message
        {
            get;
        }


        bool Success
        {
            get;
        }
        #endregion

        string GetConfigValueForKey(string key);
        void LoadDefaultsInDictionary();//load factory settings(not user customised) in memory
        void SetAllSettingsToDefault();//set all settings in config file to defaults.
        void LoadConfig();// Load user costomised configuration
        void ModifyConfig(string key, string value);// modify config
        void RefreshConfig();// Refresh afer modifying configuration

    }
}
