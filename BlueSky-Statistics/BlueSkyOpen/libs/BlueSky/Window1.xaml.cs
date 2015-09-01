using System;
using System.Collections.Generic;
using System.Windows.Controls;
using BSky.Statistics.Service.Engine.Interfaces;
using BSky.Interfaces;
using BlueSky.Services;
using Microsoft.Practices.Unity;
using BlueSky.Commands.Output;
using System.Windows;
using BSky.Lifetime.Interfaces;
using BSky.Lifetime;
using System.Windows.Threading;
using BlueSky.Commands.History;
using BlueSky.Commands.Analytics.TTest;
using System.Windows.Input;
using BSky.RecentFileHandler;
using BSky.Interfaces.Interfaces;
using BSky.Statistics.Common;
using BSky.MenuGenerator;
using System.Windows.Documents;
using System.Windows.Media;

namespace BlueSky
{
    /// <summary>
    /// Interaction logic for Window1.xaml, the main application window.
    /// </summary>
    public partial class Window1 : Window
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Jan2014

        //IDashBoardService _dashBoardService;//15Jan2013
        IAnalyticsService _analyticService;
        OutputMenuHandler omh = new OutputMenuHandler();
        CommandHistoryMenuHandler chmh = new CommandHistoryMenuHandler();//04Mar2013

        bool _IsException = false;//19feb2013there is no Exception state in the begining.
        RecentDocs recentfiles = LifetimeService.Instance.Container.Resolve<RecentDocs>();//21Dec2013

        public Window1(IUnityContainer container, IAnalyticsService analytics, IDashBoardService dashBoardService)
        {
            InitializeComponent();
            ///loading file when its double clicked ////

            try
            {

                //// rest of the old code //
                AssociateShortcutToolbarCommands();//18Mar2014
                recentfiles.recentitemclick = RecentItem_Click;//
                _analyticService = analytics;
                MainWindowMenuFactory mf = new MainWindowMenuFactory(Menu, maintoolbar, dashBoardService);
                Menu.Items.Insert(Menu.Items.Count - 2, omh.OutputMenu);//place output menu just before 2nd-last item.
                RefreshRecent();//recent menu
                Menu.Items.Insert(Menu.Items.Count - 2, chmh.CommandHistMenu);//04Mar2013 //place output menu just before 2nd-last item
            }
            catch (Exception ex)//17Jan2014
            {
                MessageBox.Show("Menus can't be generated...");
                logService.WriteToLogLevel("Menus can't be generated.\n" + ex.StackTrace, LogLevelEnum.Fatal);
                this.Close();
                return;
            }

            this.WindowState = System.Windows.WindowState.Normal;
            this.Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);
            UIControllerService layoutController = container.Resolve<UIControllerService>();

            layoutController.DocGroup = documentContainer;
            //layoutController.LayoutManager = DockContainer;
            container.RegisterInstance<IUIController>(layoutController);
            //its too early to call it here. BlueSky R pacakge is not yet loaded   SetRDefaults();//30Sep2014
            
        }

        private void AssociateShortcutToolbarCommands()//18Mar2014
        {
            bNew.Command = new BlueSky.Commands.File.FileNewCommand();
            bOpen.Command = new BlueSky.Commands.File.FileOpenCommand();
            bSave.Command = new BlueSky.Commands.File.FileSaveCommand();
            bCut.Command = ApplicationCommands.Cut;
            bCopy.Command = ApplicationCommands.Copy;
            bPaste.Command = ApplicationCommands.Paste;
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();
            logService.WriteToLogLevel("Unhandled Exception Occured in App's main window :", LogLevelEnum.Fatal, e.Exception);
            MessageBox.Show("Main Window:" + e.Exception.ToString());
            e.Handled = true;// if you false this and comment following line, then, exception goes to XAML Application Exception
            Environment.Exit(0);
        }

        public bool IsException //19feb2013 for tracking Exception state. Then show appropriate message while closing app.
        {
            set { _IsException = value; }
        }

        public OutputMenuHandler OMH
        {
            get { return omh; }
            set { omh = value; } /// check and remove if not needed
        }

        public CommandHistoryMenuHandler History //04Mar2013 accessed form outside for refreshing
        {
            get { return chmh; }
            set { chmh = value; } /// check and remove if not needed
        }

       
        #region refresh recent file list 21feb2013
        public void RefreshRecent()
        {
            MenuItem recent = GetMenuItemByHeaderPath("File>Recent");
            try
            {
                recentfiles.RecentMI = recent;
            }
            catch (Exception ex)//17Jan2014
            {
                MessageBox.Show("Recent.xml not found...");
                logService.WriteToLogLevel("Recent.xml not found.\n" + ex.StackTrace, LogLevelEnum.Fatal);
            }
        }

        //search in direction File>Open... to find specific item in path
        private MenuItem GetMenuItemByHeaderPath(string headerpath)
        {
            MenuItem mi = null;
            string[] patharr = headerpath.Split('>');// File, Open

            ///search MenuItem by searching Header
            foreach (string itm in patharr)
            {
                mi = FindItemInBranch(mi, itm);
            }

            return mi;
        }

        ///Find Item travesing thru a selected branch // this method will work with above funtion 'GetMenuByHeaderPath'
        private MenuItem FindItemInBranch(MenuItem ParentItem, string ChildHeader) //eg.. in 'File' look for 'Open'
        {
            MenuItem mi = null;
            if (ParentItem == null)//for Root node which is mainmenu
            {
                foreach (MenuItem itm in Menu.Items)
                {
                    if (itm.Header.ToString().Equals(ChildHeader))
                    {
                        mi = itm;
                        break;
                    }
                }
            }
            else
            {
                foreach (object oitm in ParentItem.Items)
                {
                    var casted = oitm as MenuItem;//if cast is possible or not
                    if (casted != null)
                    {
                        MenuItem itm = oitm as MenuItem;
                        if (itm.Header.ToString().Equals(ChildHeader))
                        {
                            mi = itm;
                            break;
                        }
                    }
                }
            }
            return mi;
        }

        private void RecentItem_Click(string fullpathfilename)
        {
            //MessageBox.Show("Finally Opening up a file ..");
            BlueSky.Commands.File.FileOpenCommand fopen = new BlueSky.Commands.File.FileOpenCommand();
            fopen.FileOpen(fullpathfilename);
        }

        #endregion

        #region Window operations
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)///15Jan2013
        {
            try
            {
                MainWindow mwindow = LifetimeService.Instance.Container.Resolve<MainWindow>();//28Jan2013
                mwindow.Activate();
                if (mwindow.OwnedWindows.Count > 1)//if any child window exists for which mwindow is owner. count 1, app-window is already open.
                {
                    System.Windows.Forms.DialogResult dresult = ExitAppMessaageBox();//19feb2013
                    if (dresult != System.Windows.Forms.DialogResult.Yes)//save & dont close
                    {
                        e.Cancel = true;
                        return;
                    }
                    #region CLose Output Windows and then Close Syntax Eiditor
                    //// Close output window and synedtr window /// 05Feb2013
                    OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
                    SyntaxEditorWindow sewindow = LifetimeService.Instance.Container.Resolve<SyntaxEditorWindow>();

                    // First collect window names
                    string[] outwinnames = new string[owc.Count];
                    int i = 0;
                    foreach (KeyValuePair<String, IOutputWindow> item in owc.AllOutputWindows)
                    {
                        outwinnames[i] = item.Key;
                        i++;
                    }
                    //close each output window one by one.
                    foreach (string winname in outwinnames)
                    {
                        (owc.GetOuputWindow(winname) as Window).Close();//invoke close {Closing then Closed }
                    }

                    //now close Syntax Editor window
                    sewindow.SynEdtForceClose = true;//This line should not appear in any other place. Forcefully closing Syn Edtr.
                    sewindow.Close();
                    if (owc.Count > 0 || sewindow != null && !sewindow.SynEdtForceClose)// if any of the output window is open
                    {
                        e.Cancel = true; // abort closing.
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("Error while closing:" + ex.Message, LogLevelEnum.Error);
            }
        }

        //load newly installed command //
        public void Window_Refresh_Menus()//15Jan2013
        {

        }

        //19feb2013 To show appropriate message while App Exit.
        private System.Windows.Forms.DialogResult ExitAppMessaageBox()
        {
            System.Windows.Forms.DialogResult dresult;

            if (!_IsException) // If App Exit when users shuts the App
            {
                dresult = System.Windows.Forms.MessageBox.Show(
                            "Do you want to close this application?",
                            "Exit Application?",
                            System.Windows.Forms.MessageBoxButtons.YesNo,
                            System.Windows.Forms.MessageBoxIcon.Question);
            }
            else // If App Exit when Exception occurs
            {
                dresult = System.Windows.Forms.MessageBox.Show(
                            "Error Occured. App will Exit.",
                            "Exiting Application!",
                            System.Windows.Forms.MessageBoxButtons.OK,
                             System.Windows.Forms.MessageBoxIcon.Error);
                dresult = System.Windows.Forms.DialogResult.Yes; /// Convert OK to YES
            }
            return dresult;
        }
        #endregion

        private void Window_Activated(object sender, EventArgs e)
        {
            //MessageBox.Show("Datagrids Active");

        }

        private void bRefreshGrids_Click(object sender, RoutedEventArgs e)//Refresh GRids
        {
            AUAnalysisCommandBase auacb = new AUAnalysisCommandBase();
            auacb.RefreshBothGrids();//16Jul2015 refrehs both grid on clicking 'refresh' icon
        }

        //30Sep2014 Refresh R side global vars etc..
        public void SetRDefaults()
        {
            IAnalyticsService analytics = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//23nov2012
            CommandRequest rcmd = new CommandRequest();

            //16Dec2013 read from config (this has priority, because it was set before closing BSky App
            string configdecidigits = confService.GetConfigValueForKey("noofdecimals");

            //Call R function to get Decimal digit. This will be used in case config does not have value set previously.
            rcmd.CommandSyntax = "BSkyGetDecimalDigitSetting()"; //get Decimal Digit
            object retres = analytics.ExecuteR(rcmd, true, false);
            string rdecidigits = retres != null ? retres.ToString() : string.Empty;

            int noofdecimaldigits;
            bool parsed;
            //Now use configdecidigits. If its not present then use rdecidigits instead.
            if (configdecidigits != null && configdecidigits.Trim().Length > 0)
            {
                parsed = Int32.TryParse(configdecidigits, out noofdecimaldigits);
            }
            else
            {
                parsed = Int32.TryParse(rdecidigits, out noofdecimaldigits);
            }

            //Call R function to set Decimal digit either from "C# config" or from R defaults.
            rcmd.CommandSyntax = "BSkySetDecimalDigitSetting(decimalDigitSetting = " + noofdecimaldigits + ")"; //Set Decimal Digit
            retres = analytics.ExecuteR(rcmd, false, false);
        }


        #region Statusbar custom message (messages other than license info)
        string bakupstatmsg = "For additional functionality and for details on the commercial edition, ";
        public void setLMsgInStatusBar(string message)
        {
            if (message == null || message.Trim().Length < 1)
                message = bakupstatmsg; // putting back license message

            licstatus.Text = message;

            if (message.Contains("For additional functionality"))
                licstatus.Inlines.Add(new Run("click here.") { TextDecorations = TextDecorations.Underline, Foreground = Brushes.Blue });
        }
        #endregion

        private void licstatus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.blueskystatistics.com/category-s/118.htm");
        }
    }
}
