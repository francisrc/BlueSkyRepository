using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using BSky.Lifetime.Interfaces;

namespace BSky.Lifetime.Services
{
    public class ConfigService : IConfigService
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();
        public ConfigService()
        {
            _success = true;
            _message = string.Empty;
            LoadConfig();//load current settings from config file, to show in UI
            LoadDefaultsInDictionary(); //load factory settings in memory for resetting to defaults if needed

        }

        private void LoadCustomAppConfigurations()
        {
            #region
            //string appFullPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //string myconfigFile = System.IO.Path.Combine(appFullPath, "App.config");
            //ExeConfigurationFileMap myconfigFileMap = new ExeConfigurationFileMap();
            //myconfigFileMap.ExeConfigFilename = myconfigFile;
            //System.Configuration.Configuration myconfig = ConfigurationManager.OpenMappedExeConfiguration(myconfigFileMap, ConfigurationUserLevel.None);

            //myconfig.AppSettings.Settings["key"].Value = "New Value";
            //myconfig.Save();
            #endregion

            #region
            //// Open App.Config of executable
            //System.Configuration.Configuration config =
            // ConfigurationManager.OpenExeConfiguration
            //            (ConfigurationUserLevel.None);

            //// Add an Application Setting.
            //config.AppSettings.Settings.Add("ModificationDate",
            //               DateTime.Now.ToLongTimeString() + " ");

            //// Save the changes in App.config file.
            //config.Save(ConfigurationSaveMode.Modified);

            //// Force a reload of a changed section.
            //ConfigurationManager.RefreshSection("appSettings");

            ///// Diplay new values ////
            //foreach (string key in ConfigurationManager.AppSettings)
            //{
            //    string value = ConfigurationManager.AppSettings[key];
            //    Console.WriteLine("Key: {0}, Value: {1}", key, value);
            //}
            #endregion
        }

        #region Interface

        #region Properties
        NameValueCollection _appSettings;
        public NameValueCollection AppSettings
        {
            get { return _appSettings; }
        }

        Dictionary<string, string> _DefaultSettings = new Dictionary<string, string>();
        public Dictionary<string, string> DefaultSettings
        {
            get { return _DefaultSettings; }
        }

        string _message;
        public string Message
        {
            get { return _message; }
        }

        bool _success;
        public bool Success
        {
            get { return _success; }
        }
        #endregion

        #region methods

        // Try to get value of key first from user's custom setting, if not found, then from default settings(hardcoded).
        // If key not exists in custom or default then write error in log and return empty string
        public string GetConfigValueForKey(string key)
        {
            string val = this.AppSettings.Get(key);
            if (val == null || val.Trim().Length < 1)//if user's key is invalid(not exists).OR value is empty
            {
                if(_DefaultSettings.ContainsKey(key))//if key exists in default config
                    val = _DefaultSettings[key];
                else //if user's key is invalid(not exists).OR default value is empty
                {
                    val = string.Empty;
                    logService.WriteToLogLevel("\'" + key + "\'" + " Config Key Not found. OR, Value not set", LogLevelEnum.Error);
                }
            }

            return val;
        }

        public void LoadDefaultsInDictionary()
        {
            string tempDir = System.IO.Path.GetTempPath().Replace("\\","/");//C:\Users\Anil\AppData\Local\Temp\
            //create default path for all in _appSettings
            _DefaultSettings.Clear();
            //hardcoded defaults for each key
            //_DefaultSettings.Add("tempfolder", tempDir);// temporary folder 
            _DefaultSettings.Add("tempfolder", "");// temporary folder 
            //_DefaultSettings.Add("tempimage", tempDir + "rimage.png");//temp image, loading output 
            _DefaultSettings.Add("tempimage", "rimage.png");//only filename so that we can add different paths to it for different users.
            _DefaultSettings.Add("outputstub", "false");// false for testing output template no data populates
            //_DefaultSettings.Add("tempsink", tempDir + "mymsg.txt");//temp sink full path filename
            _DefaultSettings.Add("tempsink", "rsink.txt");//only filename so that we can add different paths to it for different users.
            //_DefaultSettings.Add("sinkimage", tempDir + "image%03d.png");//syn edt generated image 
            _DefaultSettings.Add("sinkimage", "image%03d.png");//only filename so that we can add different paths to it for different users.
            _DefaultSettings.Add("sinkregstrdgrph", "./Config/GraphicCommandList.txt");//syn edt registered graphic command list
            //_DefaultSettings.Add("bskygrphcntrlimage", tempDir + "newImage.png");//graphic conrtole image
            _DefaultSettings.Add("bskygrphcntrlimage", "newImage.png");//only filename so that we can add different paths to it for different users.
            _DefaultSettings.Add("loglevel", "Error");//Default Log level
            _DefaultSettings.Add("InitialDirectory", "");//Default File open location
            _DefaultSettings.Add("noofdecimals", "2");//No of decimals to show in C1Flexgrid
            _DefaultSettings.Add("nooftreechars", "10");//No of chars to show in title in left tree in output
            _DefaultSettings.Add("loadSavMissingValue", "false");//Load or not SAV file's missing value attribute
            _DefaultSettings.Add("openDatasetOption", "false");//A popup will appear asking for options just before opening dataset.
            _DefaultSettings.Add("fake", "Do Nothing");//for testing only
            //_DefaultSettings.Add("test", "myimg.png");
            _DefaultSettings.Add("dctitlecol", "#FF808080");//dialog command title color
            _DefaultSettings.Add("syntitlecol", "#FF000000");//batch command title color
            _DefaultSettings.Add("rcommcol", "#FF0000FF");//R command color
            _DefaultSettings.Add("errorcol", "#FF800000");//Error color
            _DefaultSettings.Add("outputmousehovercol", "#FFFF8C00");//output window: mouse over any control shows a colored box around it
            _DefaultSettings.Add("navtreeselectedcol", "#FFFFD900");//output window: click nav tree leaf item shows a colored box around the cotrol, referenced by this leaf.
            _DefaultSettings.Add("numericrowheaders", "false");//For showing numeric row headers in output C1Flexgrid
            _DefaultSettings.Add("imagewidth", "600");// image width in output
            _DefaultSettings.Add("imageheight", "600");//image height in output
            _DefaultSettings.Add("daysleftreminder", "3,7,15,30");//Lic expire reminder 
            _DefaultSettings.Add("maxfactorcount", "20");
            _DefaultSettings.Add("advancedlogging", "false");//Advanced Logging switch

        }

        public void SetAllSettingsToDefault()
        {
            string vals;
            _success = true;
            try
            {
                Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                foreach (string key in _appSettings.Keys)
                {
                    if (_DefaultSettings.ContainsKey(key))
                        vals = _DefaultSettings[key];
                    else
                    {
                        vals = "";
                        _success = false;
                        _message = "Key not found : " + key;
                    }
                    configuration.AppSettings.Settings[key].Value = vals;
                }
                configuration.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("Can't set defaults in config ", LogLevelEnum.Error);
                logService.WriteToLogLevel(ex.Message, LogLevelEnum.Error);
            }
        }

        public void LoadConfig()// user config file
        {
            #region
            _success = true;
            try
            {
                // Get the AppSettings section.
                _appSettings = ConfigurationManager.AppSettings;

                // Get the AppSettings section elements.
                if (_appSettings.Count == 0)
                {
                    _message = ("AppSettings is empty!");
                    _success = false;
                    logService.WriteToLogLevel(_message, LogLevelEnum.Error);
                    return;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                logService.WriteToLogLevel(e.ToString(), LogLevelEnum.Error);
            }
            #endregion
        }

        public void ModifyConfig(string key, string value)//modify particular key
        {
            _success = true;
            try
            {
                Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configuration.AppSettings.Settings[key].Value = value;
                configuration.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                _message = ex.Message;
                _success = false;
                logService.WriteToLogLevel(ex.Message, LogLevelEnum.Error);
            }
        }

        public void RefreshConfig()//refresh configuration with new values.
        {
            string vals;
            _success = true;
            try
            {
                Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                foreach (string key in _appSettings.Keys)
                {
                    vals = _appSettings.Get(key);
                    if (vals.Trim().Length == 0)//load deafaults if user left the config field blank
                    {
                        if (_DefaultSettings.ContainsKey(key))
                            vals = _DefaultSettings[key];
                        else
                        {
                            vals = "";
                            _success = false;
                            _message = "Key not found : " + key;
                        }
                    }
                    configuration.AppSettings.Settings[key].Value = vals;
                }
                configuration.Save();//saving back to file
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel(ex.ToString(), LogLevelEnum.Error);
            }
        }

        #endregion

        #endregion


    }

}
