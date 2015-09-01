using System;
using BlueSky.CommandBase;
using BSky.Lifetime.Interfaces;
using BSky.Lifetime;
using BlueSky.Windows;
using Microsoft.Practices.Unity;
using BSky.Statistics.Common;
using System.Windows;
using BSky.RecentFileHandler;
using BSky.Interfaces.Interfaces;

namespace BlueSky.Commands.File
{
    class FileNewCommand : BSkyCommandBase
    {
        protected override void OnPreExecute(object param)
        {
        }

        public const String FileNameFilter = "IBM SPSS (*.sav)|*.sav| Excel 2003 (*.xls)|*.xls|Excel 2007-2010 (*.xlsx)|*.xlsx|Comma Seperated (*.csv)|*.csv|DBF (*.dbf)|*.dbf|R Obj (*.RData)|*.RData";
        IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//12Dec2013
        RecentDocs recentfiles = LifetimeService.Instance.Container.Resolve<RecentDocs>();//21Dec2013

        protected override void OnExecute(object param)
        {

            //// Get initial Dir 12Feb2013 ////
            string initDir = confService.GetConfigValueForKey("InitialDirectory");
                // Start Some animation for loading dataset ///
                //DatasetLoadingBusyWindow bw = new DatasetLoadingBusyWindow("Please wait while Dataset is Loading...");
                //bw.Show();
                //bw.Activate();
                //bw.Close();//Comment this line after testing and uncomment one below FileOpen
                //some code from here moved to FileOpen //
                NewFileOpen("");//openFileDialog.FileName);

                /// Stop the animation after loading ///
                //bw.Close();

                ///Set initial Dir. 12Feb2013///
               // initDir = Path.GetDirectoryName("");//openFileDialog.FileName);
               // confService.ModifyConfig("InitialDirectory", initDir);
               // confService.RefreshConfig();

        }



        protected override void OnPostExecute(object param)
        {
        }

        public void NewFileOpen(string filename)//21Feb 2013 For opening Dataset from File > Open & File > Recent
        {
            //if (filename != null && filename.Length > 0)
            {
                IUnityContainer container = LifetimeService.Instance.Container;
                IDataService service = container.Resolve<IDataService>();
                IUIController controller = container.Resolve<IUIController>();
                Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();//for refeshing recent files list
                //if (System.IO.File.Exists(filename))
                {

                    DataSource ds = service.NewDataset();//filename);
                    if (ds != null)
                    {
                        controller.LoadNewDataSet(ds);
                        //recentfiles.AddXMLItem(filename);//adding to XML file for recent docs
                    }
                    else
                    {
                        MessageBox.Show(appwindow, "Unable to open " + filename +
                            ".\nReasons could be:\nRequired R package is not installed.\nFormat not supported.\nOR.\nR.Net server from old session still running. Use task manager to close it." +
                            "\nOR.\n Some issue on R side (like: required library not loaded).");
                        SendToOutputWindow( "Error Opening Dataset", filename);
                    }
                }
                //else
                //{
                //    MessageBox.Show(filename + " does not exists!", "File not found!", MessageBoxButton.OK, MessageBoxImage.Warning);
                //    //If file does not exists. It should be removed from the recent files list.
                //    recentfiles.RemoveXMLItem(filename);
                //}
                //18OCt2013 move up for using in msg box   Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();//for refeshing recent files list
                appwindow.RefreshRecent();
            }
            //08Apr2015 bring main window in front after file open, instead of output window
            Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();
            window.Activate();
        }


    }
}
