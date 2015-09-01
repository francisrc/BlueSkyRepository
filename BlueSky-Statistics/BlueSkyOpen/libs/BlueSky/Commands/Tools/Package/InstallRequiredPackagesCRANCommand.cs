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
    //Installs required packages from CRAN. XML file is maintains a list of packages
    class InstallRequiredPackagesCRANCommand : BSkyCommandBase
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
                appwindow.setLMsgInStatusBar("Please wait ... Installing required R packages from CRAN...");
                ShowMouseBusy();
                //Get list of required pacakges from RequiredPackages.xml
                List<string> reqPkgList = GetReqRPackageList();
                PackageHelperMethods phm = new PackageHelperMethods();
                UAReturn r = null;
                foreach (string pkgname in reqPkgList)
                {
                    r = phm.InstallReqPackageFrmCRAN(pkgname);
                    if (r != null)
                    {
                        // It is not error message. It could be success/failure msg. A status message basically.
                        SendToOutputWindow("Package(s) Installation Status:", r.Error);
                    }

                }
                ShowMouseFree();
            }
            catch (Exception ex)
            {
                ShowMouseFree();
                MessageBox.Show("Error while installing required packages from CRAN.", "Error Occurred!");
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

        #region Get The required R pacakge list
        private List<string> GetReqRPackageList()
        {
            //read RequiredPackage.xml from Config and prepare a list
            XMLitemsProcessor requiredpackages = new XMLitemsProcessor();
            requiredpackages.MaxRecentItems = 1000;
            requiredpackages.XMLFilename = string.Format(@"{0}RequiredPackages.xml", BSkyAppData.BSkyDataDirConfigFwdSlash);
            requiredpackages.RefreshXMLItems();
            List<string> rpkglist = requiredpackages.RecentFileList;

            return rpkglist;
        }
        #endregion
    }
}
