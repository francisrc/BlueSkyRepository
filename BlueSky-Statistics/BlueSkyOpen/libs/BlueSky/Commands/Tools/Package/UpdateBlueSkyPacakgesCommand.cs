using System;
using System.Windows;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using Microsoft.Win32;
using BSky.Statistics.Common;
using BlueSky.CommandBase;

namespace BlueSky.Commands.Tools.Package
{
    //initially it was copied from InstallPackageCommand.cs //04May2015
    class UpdateBlueSkyPacakgesCommand : BSkyCommandBase
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();
        protected override void OnPreExecute(object param)
        {

        }
        public const String FileNameFilter = "Package (BlueSky*.zip)|BlueSky*.zip";
        protected override void OnExecute(object param)
        {
            //Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();
            try
            {
                bool autoLoad = true, overwrite = true;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                //// Get initial Dir ////
                //string initDir = confService.GetConfigValueForKey("InitialDirectory");
                //openFileDialog.InitialDirectory = initDir;
                openFileDialog.Filter = FileNameFilter;
                openFileDialog.Multiselect = true;
                bool? output = openFileDialog.ShowDialog(Application.Current.MainWindow);
                if (output.HasValue && output.Value)
                {
                    string[] pkgfilenames = openFileDialog.FileNames;
                    PackageHelperMethods phm = new PackageHelperMethods();
                    UAReturn r = phm.PackageFileInstall(pkgfilenames, autoLoad, overwrite);// PackageFileInstall(pkgfilenames);//openFileDialog.FileName);
                    if (r != null && r.Success)
                    {
                        SendToOutputWindow("Install Package", r.SimpleTypeData.ToString());
                        MessageBox.Show("Please restart BlueSky Application to use the updated BlueSky package(s)", "Restart BlueSky Application", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        if (r != null)
                        {
                            string msg = r.SimpleTypeData as string;
                            SendToOutputWindow("Error Installing Package", msg);
                        }
                    }
                    ///Set initial Dir.///
                    //initDir = Path.GetDirectoryName(openFileDialog.FileName);
                    //confService.ModifyConfig("InitialDirectory", initDir);
                    //confService.RefreshConfig();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while installing package.", "Error Occurred!");
                logService.WriteToLogLevel("Error:", LogLevelEnum.Error, ex);
            }
        }

        protected override void OnPostExecute(object param)
        {

        }

    }
}
