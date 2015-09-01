using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlueSky.CommandBase;
using BSky.Lifetime.Interfaces;
using BSky.Lifetime;
using BlueSky.Commands.Tools.Package.Dialogs;
using System.Windows;
using System.IO;
using BSky.Statistics.Common;
using System.Windows.Input;

namespace BlueSky.Commands.Tools.Package
{
    class InstallRequiredPackagesCommand : BSkyCommandBase
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012
        protected override void OnPreExecute(object param)
        {

        }
        public const String FileNameFilter = "Package (*.zip)|*.zip";
        protected override void OnExecute(object param)
        {
            Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();
            try
            {
                //Now trying to install R .zip packages from local drive (minimum required R packages)
                InstallRequiredPackagesFromZip instRzip = new InstallRequiredPackagesFromZip();
                instRzip.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                instRzip.ShowDialog();
                string zipPath = instRzip.SelectedZipPath;
                if (zipPath.Length > 0)
                {
                    if (Directory.Exists(zipPath))
                    {
                        string[] zipFiles = null;
                        appwindow.setLMsgInStatusBar("Please wait ... Installing required R packages from local drive...");
                        ShowMouseBusy();
                        zipFiles = Directory.GetFiles(zipPath, "*.zip");
                        if (zipFiles == null || zipFiles.Length == 0)
                        {
                            MessageBox.Show("The path you have selected does not contain any R zip package files. Try again.", "No R package files found", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            PackageHelperMethods phm = new PackageHelperMethods();
                            UAReturn r = phm.PackageFileInstall(zipFiles);// PackageFileInstall(pkgfilenames);//openFileDialog.FileName);
                            if (r != null && r.Success)
                            {
                                SendToOutputWindow("Package(s) Installation Status:", r.SimpleTypeData.ToString());//"Install Package"
                            }
                            else
                            {
                                if (r != null)
                                {
                                    string msg = r.SimpleTypeData as string;
                                    SendToOutputWindow("Package(s) Installation Status:", msg);//"Error Installing Packages:"
                                }
                            }
                            ///Set initial Dir. 12Feb2013///
                            //initDir = Path.GetDirectoryName(openFileDialog.FileName);
                            //confService.ModifyConfig("InitialDirectory", initDir);
                            //confService.RefreshConfig();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid Path.", "Path not valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else//cancel minimum required install process
                {
                    MessageBox.Show("BlueSky application may not work properly if the required R pacakges are missing.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                ShowMouseFree();
            }
            catch (Exception ex)
            {
                ShowMouseFree();
                MessageBox.Show("Error while installing package.", "Error Occurred!");
                logService.WriteToLogLevel("Error:", LogLevelEnum.Error, ex);
            }
            appwindow.setLMsgInStatusBar("");
        }

        protected override void OnPostExecute(object param)
        {

        }


        #region Mouse Busy/Free
        Cursor defaultcursor;
        private void ShowMouseBusy()
        {
            defaultcursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }

        private void ShowMouseFree()
        {
            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
