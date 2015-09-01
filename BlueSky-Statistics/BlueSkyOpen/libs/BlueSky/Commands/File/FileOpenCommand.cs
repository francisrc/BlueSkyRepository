using System;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using System.Windows;

using BSky.Statistics.Common;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using System.IO;
using BlueSky.Windows;
using BlueSky.CommandBase;
using System.Windows.Input;
using BSky.RecentFileHandler;
using BSky.Interfaces.Interfaces;
using System.Collections.Generic;
using System.Text;

namespace BlueSky.Commands.File
{
    public class FileOpenCommand : BSkyCommandBase
    {

        protected override void OnPreExecute(object param)
        {
        }

        public const String FileNameFilter = "All Files (*.*)|*.*|IBM SPSS (*.sav)|*.sav|Excel 2003 (*.xls)|*.xls|Excel 2007-2010 (*.xlsx)|*.xlsx|Comma Seperated (*.csv)|*.csv|DBF (*.dbf)|*.dbf|R Obj (*.RData)|*.RData";
        IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//12Dec2013
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012
        RecentDocs recentfiles = LifetimeService.Instance.Container.Resolve<RecentDocs>();//21Dec2013
        //XMLitemsProcessor defaultpackges = LifetimeService.Instance.Container.Resolve<XMLitemsProcessor>();//06Nov2014

        DatasetLoadingBusyWindow bw = null;
        protected override void OnExecute(object param)
        {


            OpenFileDialog openFileDialog = new OpenFileDialog();
            //// Get initial Dir 12Feb2013 ////
            string initDir = confService.GetConfigValueForKey("InitialDirectory");
            openFileDialog.InitialDirectory = initDir;
            openFileDialog.Filter = FileNameFilter;
            Window1 appwin = LifetimeService.Instance.Container.Resolve<Window1>();
            bool? output = openFileDialog.ShowDialog(appwin);//Application.Current.MainWindow);
            if (output.HasValue && output.Value)
            {
                //some code from here moved to FileOpen //
                FileOpen(openFileDialog.FileName);

                /// Stop the animation after loading ///

                ///Set initial Dir. 12Feb2013///
                initDir = Path.GetDirectoryName(openFileDialog.FileName);
                confService.ModifyConfig("InitialDirectory", initDir);
                confService.RefreshConfig();
            }
            logService.WriteToLogLevel("Done File Loading: Now Grid Takes Over. ", LogLevelEnum.Info);

            //bring dataset window in front
            //MainWindow mwindow = LifetimeService.Instance.Container.Resolve<MainWindow>();
            //mwindow.Activate();
        }

        #region Progressbar
        Cursor defaultcursor;
        //Shows Progressbar
        private void ShowProgressbar()
        {
            //bw = new DatasetLoadingBusyWindow("Please wait while Dataset is Loading...");
            //bw.Owner = (Application.Current.MainWindow);
            //bw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //bw.Visibility = Visibility.Visible;
            //bw.Show();
            //bw.Activate();
            defaultcursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }
        //Hides Progressbar
        private void HideProgressbar()
        {
            //if (bw != null)
            //{
            //    bw.Close(); // close window if it exists
            //    //bw.Visibility = Visibility.Hidden;
            //    //bw = null;
            //}
            Mouse.OverrideCursor = defaultcursor;
        }

        //in App Main Window stausbar
        //Shows Progressbar in statusbar
        private void ShowStatusProgressbar()
        {
            Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();//27Oct2014
            //window.ProgressStatusPanel.Visibility = Visibility.Visible;
            defaultcursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }
        //Hides Progressbar in statusbar
        private void HideStatusProgressbar()
        {
            Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();//27Oct2014
            //window.ProgressStatusPanel.Visibility = Visibility.Hidden;
            Mouse.OverrideCursor = defaultcursor;
        }
        #endregion

        protected override void OnPostExecute(object param)
        {
        }

        public void FileOpen(string filename)//21Feb 2013 For opening Dataset from File > Open & File > Recent
        {
            // Start Some animation for loading dataset ///
            ShowProgressbar();//ShowStatusProgressbar();//29Oct2014 

            if (filename != null && filename.Length > 0)
            {
                IUnityContainer container = LifetimeService.Instance.Container;
                IDataService service = container.Resolve<IDataService>();
                IUIController controller = container.Resolve<IUIController>();
                Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();//for refeshing recent files list
                if (System.IO.File.Exists(filename))
                {
                    string sheetname = null;
                    if (filename.EndsWith(".xls") || filename.EndsWith(".xlsx"))//27Jan2014
                    {
                        object tbls = service.GetOdbcTableList(filename);
                        if (tbls != null)
                        {
                            SelectTableWindow stw = new SelectTableWindow();
                            string[] tlist = null;

                            if (tbls.GetType().Name.Equals("String"))
                            {
                                tlist = new string[1];
                                tlist[0] = tbls as string;
                            }
                            else if(tbls.GetType().Name.Equals("String[]"))
                            {
                                tlist = tbls as string[];
                                
                            }

                            stw.FillList(tlist);
                            HideProgressbar();
                            stw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            stw.ShowDialog();
                            ShowProgressbar();
                            if (stw.SelectedTableName == null)//cancel clicked
                            {
                                HideProgressbar();//HideStatusProgressbar();//29Oct2014 
                                return; 
                            }
                            else
                                sheetname = stw.SelectedTableName;
                        }
                    }
                    logService.WriteToLogLevel("Setting DataSource: ", LogLevelEnum.Info);
                    DataSource ds = service.Open(filename, sheetname);
                    string errormsg = string.Empty;
                    if(ds!=null && ds.Message!=null && ds.Message.Length>0) //message that is related to error
                    {
                        errormsg="\n"+ds.Message;
                        ds = null;//making it null so that we stop executing further
                    }

                    if (ds != null)//03Dec2012
                    {
                        logService.WriteToLogLevel("Start Loading: "+ ds.Name, LogLevelEnum.Info);
                        controller.LoadNewDataSet(ds);
                        logService.WriteToLogLevel("Finished Loading: "+ ds.Name, LogLevelEnum.Info);
                        recentfiles.AddXMLItem(filename);//adding to XML file for recent docs
                    }
                    else
                    {
                        HideProgressbar();

                        //Following block is not needed
                        //StringBuilder sb = new StringBuilder();
                        //List<string> defpacklist = defaultpackges.RecentFileList;
                        //foreach(string s in defpacklist)
                        //{
                        //    sb.Append(s+", ");

                        //}
                        //sb.Remove(sb.Length - 1, 1);//removing last comma
                        //string defpkgs = sb.ToString();

                        MessageBox.Show(errormsg, "Unable to open the file(Dataset)", MessageBoxButton.OK, MessageBoxImage.Warning);
                        SendToOutputWindow("Error Opening Dataset", filename +  errormsg);
                    }
                }
                else
                {
                    HideProgressbar();
                    MessageBox.Show(filename+" does not exists!", "File not found!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //If file does not exists. It should be removed from the recent files list.
                    recentfiles.RemoveXMLItem(filename);
                }
                //18OCt2013 move up for using in msg box   Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();//for refeshing recent files list
                appwindow.RefreshRecent();
            }
            HideProgressbar();// HideStatusProgressbar();//29Oct2014 

            //08Apr2015 bring main window in front after file open, instead of output window
            Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();
            window.Activate();
        }

        ////Send executed command to output window. So, user will know what he executed
        //protected override void SendToOutputWindow(string command, string title)//13Dec2013
        //{
        //    #region Get Active output Window
        //    //////// Active output window ///////
        //    OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
        //    OutputWindow ow = owc.ActiveOutputWindow as OutputWindow; //get currently active window
        //    #endregion
        //    ow.AddMessage(command, title);
        //}

        public bool OpenDataframe(string dframename)
        {
            ShowProgressbar();//ShowStatusProgressbar();//29Oct2014
            bool isSuccess = false;
            IUnityContainer container = LifetimeService.Instance.Container;
            IDataService service = container.Resolve<IDataService>();
            IUIController controller = container.Resolve<IUIController>(); 
            Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();//for refeshing recent files list

            string filename = controller.GetActiveDocument().FileName;
            //For Excel
            string sheetname = controller.GetActiveDocument().SheetName;
            if (sheetname == null) sheetname = string.Empty;
            // if dataset was already loaded last time then this time we want to refresh it
            bool isDatasetNew = service.isDatasetNew(dframename + sheetname);
            try
            {
                DataSource ds = service.OpenDataframe(dframename, sheetname);
                string errormsg = string.Empty;
                if (ds != null && ds.Message != null && ds.Message.Length > 0) //message that is related to error
                {
                    errormsg = "\n" + ds.Message;
                    ds = null;//making it null so that we do execute further
                }
                if (ds != null)//03Dec2012
                {
                    logService.WriteToLogLevel("Start Loading Dataframe: " + ds.Name, LogLevelEnum.Info);
                    if (isDatasetNew)
                        controller.Load_Dataframe(ds);
                    else
                        controller.RefreshBothGrids(ds);//23Jul2015 .RefreshGrids(ds);//.RefreshDataSet(ds);
                    ds.Changed = true; // keep track of change made, so that it can prompt for saving while closing dataset tab.
                    logService.WriteToLogLevel("Finished Loading Dataframe: " + ds.Name, LogLevelEnum.Info);
                    //recentfiles.AddXMLItem(dframename);//adding to XML file for recent docs
                    isSuccess = true;
                }
                else
                {
                    HideProgressbar();
                    MessageBox.Show(appwindow, "Unable to open '" + dframename +"'..."+
                        "\nReasons could be one or more of the following:"+
                        "\n1. Not a data frame object."+
                        "\n2. File format not supported (or corrupt file or duplicate column names)." +
                        "\n3. R.Net server from the old session still running (use task manager to kill it)." +
                        "\n4. Some issue on R side (like: required library not loaded, incorrect syntax).",
                        "Warning",MessageBoxButton.OK, MessageBoxImage.Warning);
                    SendToOutputWindow("Error Opening Dataset.(probably not a data frame)", dframename+errormsg);
                }
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("Error:" + ex.Message, LogLevelEnum.Error);
            }
            finally
            {
                //18OCt2013 move up for using in msg box   Window1 appwindow = LifetimeService.Instance.Container.Resolve<Window1>();//for refeshing recent files list
                //appwindow.RefreshRecent();
                HideProgressbar();//HideStatusProgressbar();//29Oct2014 
            }

            //if (isSuccess)
            //{
                //08Apr2015 bring main window in front after file open, instead of output window
                //Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();
                //window.Activate();
            //}
            return isSuccess;
        }


    }
}
