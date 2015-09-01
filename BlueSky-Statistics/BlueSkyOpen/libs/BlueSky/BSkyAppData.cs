using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BlueSky
{
    public static class BSkyAppData
    {
        //ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();

        private static string BSkyFolder = "BlueSky Statistics";
        private static string BSkyConfig = "Config";

        //LocalApplicationData folder -> C:\\Users\\AD\\AppData\\Local
        //ApplicationData folder -> C:\\Users\\AD\\AppData\\Roaming
        //CommonApplicationData folder -> C:\\ProgramData
        //MyDocuments folder D:\\Work\\MyDocuments
        private static string BSkyMyDocuments
        {
            get
            {
                //string MyDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                //string MyDocsFwdSlashPath = MyDocs.Replace(@"\", @"/");
                //string path = string.Format(@"{0}/{1}/", MyDocsFwdSlashPath, BSkyFolder);
                // if (PathExists(path)) // if this path does not exists, means BSky app is not installed & is running from V-Studio
                //    return path;
                //else
                return "./"; // or return current location ie from where the exe launched. Will be used when Bsky executed from VS
            }
        }

        #region Root Folder of BlueSky application in MyDocuments for Data
        // returns BSky data root path (MyDocuments/BlueSky Statistics/) having forward slashes (Unix style)
        public static string BSkyDataDirRootFwdSlash
        {
            get { return BSkyMyDocuments; }
        }

        // returns BSky data root path (MyDocuments\BlueSky Statistics\)  having back slashes (DOS style)
        public static string BSkyDataDirRootBkSlash
        {
            get { return BSkyMyDocuments.Replace(@"/", @"\"); }
        }
        #endregion

        #region Path With 'Config'(that contains XAML/XML)
        // returns BSky 'Config' path (MyDocuments/BlueSky Statistics/Config/)  having forward slashes (Unix style)
        public static string BSkyDataDirConfigFwdSlash
        {
            get { return string.Format(@"{0}{1}/", BSkyDataDirRootFwdSlash, BSkyConfig); }
        }

        // returns BSky 'Config' path (MyDocuments\BlueSky Statistics\Config\) having back slashes (DOS style)
        public static string BSkyDataDirConfigBkSlash
        {
            get { return string.Format(@"{0}{1}\", BSkyDataDirRootBkSlash, BSkyConfig); }
        }
        #endregion

        //Checks if path exists or not. Tries to create the path. If exists or created successfully, return true else returns false.
        private static bool PathExists(string dirpath)
        {
            bool locationExists = true;
            try
            {
                if (!Directory.Exists(dirpath)) // if directory does not exists, create it
                {
                    //dont want to create this path as BlueSky setup should do this instead.
                    //Directory.CreateDirectory(dirpath);
                    locationExists = false;
                }

            }
            catch (Exception ex)
            {

            }
            return (locationExists);
        }
    }

	
}
