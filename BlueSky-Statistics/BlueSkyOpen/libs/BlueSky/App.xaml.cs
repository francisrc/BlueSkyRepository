using System;
using System.Windows;
using Microsoft.Practices.Unity;
using BSky.ServerBridge;
using BSky.Interfaces;
using BlueSky.Services;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using BSky.Lifetime.Services;
using BSky.RecentFileHandler;
using BSky.Interfaces.Interfaces;
using System.Text;
using System.Windows.Input;
using BlueSky.Windows;
using System.Reflection;
using BSky.Statistics.Service.Engine.Interfaces;
using BlueSky.Commands.File;
using System.IO;


namespace BlueSky
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        UnityContainer container = new UnityContainer();

        public App()
        {
            //Calling order found was:: Dispatcher -> Main Window -> XAML Application Dispatcher -> Unhandeled
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Dispatcher.UnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);
            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Application_DispatcherUnhandledException);
            MainWindow mwindow = container.Resolve<MainWindow>();
            container.RegisterInstance<MainWindow>(mwindow);///new line
            mwindow.Show();
            mwindow.Visibility = Visibility.Hidden;

            ShowProgressbar();//strat showing progress bar
            bool BlueSkyFound = true;//Assuming BlueSky R package is present.

            LifetimeService.Instance.Container = container;
            container.RegisterInstance<ILoggerService>(new LoggerService());/// For Application log. Starts with default level "Error"
            container.RegisterInstance<IConfigService>(new ConfigService());//For App Config file
            container.RegisterInstance<IAdvancedLoggingService>(new AdvancedLoggingService());//For Advanced Logging
            ////////////// TRY LOADING BSKY R PACKAGES HERE  /////////

            ILoggerService logService = container.Resolve<ILoggerService>();
            logService.SetLogLevelFromConfig();//loading log level from config file
            logService.WriteToLogLevel("R.Net,Logger and Config loaded:", LogLevelEnum.Info);/// 

            ////Recent default packages. This code must appear before loading any R package. (including uadatapackage)
            XMLitemsProcessor defaultpackages = container.Resolve<XMLitemsProcessor>();//06Feb2014
            defaultpackages.MaxRecentItems = 50;
            defaultpackages.XMLFilename = string.Format(@"{0}DefaultPackages.xml", BSkyAppData.BSkyDataDirConfigFwdSlash);//23Apr2015 @"./Config/DefaultPackages.xml";
            defaultpackages.RefreshXMLItems();
            container.RegisterInstance<XMLitemsProcessor>(defaultpackages);

            //Recent user packages. This code must appear before loading any R package. (including uadatapackage)
            RecentItems userpackages = container.Resolve<RecentItems>();//06Feb2014
            userpackages.MaxRecentItems = 50;
            userpackages.XMLFilename = string.Format(@"{0}UserPackages.xml", BSkyAppData.BSkyDataDirConfigFwdSlash);//23Apr2015 @"./Config/UserPackages.xml";
            userpackages.RefreshXMLItems();
            container.RegisterInstance<RecentItems>(userpackages);

            try
            {
                BridgeSetup.ConfigureContainer(container);
            }
            catch (Exception ex)
            {
                string s1 = "\n1. R is installed. BlueSky Statistics requires R.";
                string s2 = "\n2. Binary incompatibility between BlueSky Statistics and R. 64bit BlueSky Statistics requires 64bit R and 32bit BlueSky Statistics required 32bit R. (Go to Help > About in BlueSky Statistics)";
                string s3 = "\n3. Another session of the BlueSky application is not already running.";
                MessageBox.Show("Please make sure:"+s1+s2+s3, "Error: Can't Launch BlueSky Application!", MessageBoxButton.OK, MessageBoxImage.Stop);
                logService.WriteToLogLevel("Unable to launch the BlueSky Application."+s1+s3, LogLevelEnum.Error);
                Environment.Exit(0);
            }
            finally 
            {
                HideProgressbar();
            }
            container.RegisterInstance<IDashBoardService>(container.Resolve<XmlDashBoardService>());
            container.RegisterInstance<IDataService>(container.Resolve<DataService>());

            IOutputWindowContainer iowc = container.Resolve<OutputWindowContainer>();
            container.RegisterInstance<IOutputWindowContainer>(iowc);

            SessionDialogContainer sdc = container.Resolve<SessionDialogContainer>();//13Feb2013
            //Recent Files settings
            RecentDocs rdoc = container.Resolve<RecentDocs>();//21Feb2013
            rdoc.MaxRecentItems = 7;
            rdoc.XMLFilename = string.Format(@"{0}Recent.xml", BSkyAppData.BSkyDataDirConfigFwdSlash);
            container.RegisterInstance<RecentDocs>(rdoc);

            Window1 window = container.Resolve<Window1>();
            container.RegisterInstance<Window1>(window);///new line
            window.Closed += new EventHandler(window_Closed);  //28Jan2013                                                      
            window.Owner = mwindow;     //28Jan2013     

            window.Show();
            window.Activate();
            ShowMouseBusy();//02Apr2015 show mouse busy
            //// one Syntax Editor window for one session ////29Jan2013
            SyntaxEditorWindow sewindow = container.Resolve<SyntaxEditorWindow>();
            container.RegisterInstance<SyntaxEditorWindow>(sewindow);///new line
            sewindow.Owner = mwindow;

            //load default packages
            window.setLMsgInStatusBar("Please wait ... Loading required R packages ...");
            IAnalyticsService IAService = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            BridgeSetup.LoadDefaultRPackages(IAService);
            string PkgLoadStatusMessage = BridgeSetup.PkgLoadStatusMessage;
            if (PkgLoadStatusMessage != null && PkgLoadStatusMessage.Trim().Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                string[] defpacklist = PkgLoadStatusMessage.Split('\n');
                foreach (string s in defpacklist)
                {
                    if (s != null && (s.ToLower().Contains("error") ))//|| s.ToLower().Contains("warning")))
                    {
                        sb.Append(s.Replace("Error loading R package:", "") + "\n");
                    }

                }
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);//removing last comma
                    string defpkgs = sb.ToString();
                    string firstmsg = "Error loading following R package(s):\n\n";
                    string msg = "\n\nInstall required R packages from CRAN by clicking:\nTools > Package > Install required package(s) from CRAN.";// +

                    HideMouseBusy();
                    MessageBox.Show(firstmsg + defpkgs + msg, "Error: Required R Package(s) Missing", MessageBoxButton.OK, MessageBoxImage.Warning);

                    if(defpkgs.Contains("BlueSky"))
                        BlueSkyFound = false;
                }
            }

            //deimal default should be set here as now BlueSky is loaded
            window.SetRDefaults();
            IAdvancedLoggingService advlog = container.Resolve<IAdvancedLoggingService>(); ;//01May2015
            advlog.RefreshAdvancedLogging();

            HideMouseBusy();//02Apr2015 hide mouse busy
            if (BlueSkyFound)
            {
                try
                {
                    //Try loading empty dataset(newdataset) just after app finished loading itself and R packages.
                    FileNewCommand newds = new FileNewCommand();
                    newds.NewFileOpen("");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Loading new dataset. Make sure you have BlueSky R package installed", "BlueSky pacakge missing", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            window.setLMsgInStatusBar("For additional functionality and for details on the commercial edition, ");
        }

        // App-win closed. Now close invisible owner(parent).
        void window_Closed(object sender, EventArgs e)//28Jan2013
        {
            MainWindow mwindow = LifetimeService.Instance.Container.Resolve<MainWindow>();
            mwindow.Close();//close invisible parent and all child windows(app-window, out-win, syntax-win) should go.
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();
            logService.WriteToLogLevel("Unhandled Exception Occured :", LogLevelEnum.Fatal, e.ExceptionObject as Exception);
            MessageBox.Show("Unhandled:"+e.ExceptionObject.ToString(), "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            GracefulExit();
            Environment.Exit(0);//We can remove this and can try to recover APP and keep it running.
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {

        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();
            logService.WriteToLogLevel("XAML Application Dispatcher Unhandled Exception Occured :", LogLevelEnum.Fatal, e.Exception);
            MessageBox.Show("XAML Application Dispatcher:" + e.Exception.ToString());
            e.Handled = true;/// if you false this and comment following line, then, exception goes to Unhandeled
            GracefulExit();
            Environment.Exit(0);//We can remove this and can try to recover APP and keep it running.
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();
            logService.WriteToLogLevel("Dispatcher Unhandled Exception Occured :", LogLevelEnum.Fatal, e.Exception);
            MessageBox.Show("Dispatcher:" + e.Exception.ToString(), "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;/// if you false this and comment following line, then, exception goes to App's main window
            GracefulExit();
            Environment.Exit(0); //We can remove this and can try to recover APP and keep it running.
        }

        //19Feb2013 For closing mainwindow which should close child and each child should ask for "save". This is not working this way.
        private void GracefulExit()
        {
            Window1 mwindow = LifetimeService.Instance.Container.Resolve<Window1>();
            mwindow.IsException = true;
            mwindow.Close();
        }


        #region Progressbar
        SplashWindow sw = null;
        Cursor defaultcursor;
        //Shows Progressbar
        private void ShowProgressbar()
        {
            sw = new SplashWindow("Please wait. Loading BSky Environment...");
            sw.Owner = (Application.Current.MainWindow);
            sw.Show();
            sw.Activate();
            defaultcursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }
        //Hides Progressbar
        private void HideProgressbar()
        {
            if (sw != null)
            {
                sw.Close(); // close window if it exists
            }
            Mouse.OverrideCursor = defaultcursor;
        }

        #endregion

        #region Mouse Busy
        //Shows busy mouse
        private void ShowMouseBusy()
        {
            defaultcursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }
        //Hides busy mouse
        private void HideMouseBusy()
        {
            Mouse.OverrideCursor = null;
        }
        #endregion


    }
}
