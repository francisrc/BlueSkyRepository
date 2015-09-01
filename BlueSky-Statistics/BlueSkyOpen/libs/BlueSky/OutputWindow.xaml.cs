using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using BSky.Interfaces.Model;
using System.Collections.ObjectModel;
using BSky.Interfaces.Commands;
using BlueSky.Services;
using BSky.Interfaces.Controls;
using BSky.Controls;
using System.IO;
using System.Text;
using System;
using BlueSky.Commands.Output;
using System.Windows.Media;
using BSky.Controls.Controls;
using System.Windows.Media.Imaging;
using ICSharpCode.SharpZipLib.Zip;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using BSky.OutputGenerator;
using System.Globalization;
using MSExcelInterop;
using BSky.Interfaces.Interfaces;
using C1.WPF.FlexGrid;
using Microsoft.Win32;
using BlueSky.Windows;
using BSky.RecentFileHandler;
using BlueSky.Commands.History;
using BSky.Interfaces.DashBoard;
using BSky.Interfaces;
using BSky.MenuGenerator;
using BSky.Interfaces.Services;

namespace BlueSky
{

    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>

    public partial class OutputWindow : Window, IOutputWindow
    {
        ObservableCollection<AnalyticsData> outputDataList = new ObservableCollection<AnalyticsData>();
        ObservableCollection<AnalyticsData> SynEdtDataList = new ObservableCollection<AnalyticsData>();//08Aug2012
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//13Dec2012
        IConfigService confService = null;//23nov2012
        MSExportToExcel _MSExcelObj; 
        SyntaxEditorWindow sewindow = null;
        RecentDocs recentSyntaxfiles = null;//19May2015
        CommandHistoryMenuHandler chmh = new CommandHistoryMenuHandler();//17Jul2015

        int imgnamecounter;//11Sep2012

        public CommandHistoryMenuHandler History //17Jul2015 accessed form outside for refreshing
        {
            get { return chmh; }
            set { chmh = value; } /// check and remove if not needed
        }
        public OutputWindow()
        {
            InitializeComponent();
            confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//23nov2012
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            _MSExcelObj = new MSExportToExcel(); //initialize
            //AllControlsPointToSameMSExcel();
            //menu1.Items.Insert(menu1.Items.Count - 1, chmh.CommandHistMenu);//17Jul2015 //place output menu just before last item

            #region Dialog toolbar
            OutputWindowMenuFactory mf = new OutputWindowMenuFactory(menu1, dialogtoolbar);//, dashBoardService);
            #endregion
            menu1.Items.Insert(menu1.Items.Count - 1, chmh.CommandHistMenu);//17Jul2015 //place output menu just before last item

            #region Syntax section related init (Expand/Collapse , Vertical/Horizontal , Find/Replace , Recent Scripts etc..)
            sbfultxt = new StringBuilder();
            initRecentSyntaxFileHandler();
            #endregion
        }

        //21Jul2015 Get DashBoardItems for creating toolbar icons
        private void CreateToolbarIcons()
        {
            IDashBoardService dashBoardService = LifetimeService.Instance.Container.Resolve<IDashBoardService>();
            List<DashBoardItem> dbis = dashBoardService.GetDashBoardItems();//Creates menu from menu.xml
            foreach (DashBoardItem dbi in dbis)
            {
                AddToolbarIcon(dbi);
            }
        }

        //21Jun2015 Adds analysis dialog icon to toolbar
        private void AddToolbarIcon(DashBoardItem item)
        {
            if (item.Items!=null && item.Items.Count > 0)//Parent Node
            {
                foreach (DashBoardItem dbi in item.Items)
                {
                    AddToolbarIcon(dbi);
                }
            }
            else //it was a child node so crete icon if icon attributes are present
            {
                if (item.showshortcuticon)
                {
                    string icontooltip = item.Name;
                    string imgsource = item.iconfullpathfilename;
                    if (!File.Exists(imgsource))
                    {
                        imgsource = "images/noimage.png";
                    }

                    Button iconButton = new Button();
                    iconButton.Command = item.Command;//Just click event to call AUAnalysisCommandBase
                    iconButton.CommandParameter = item.CommandParameter;//all XAML XML info
                    StackPanel sp = new StackPanel();

                    #region create icon
                    Image iconImage = new Image();
                    iconImage.ToolTip = icontooltip;
                    var bitmap = new BitmapImage();
                    try
                    {
                        var stream = File.OpenRead(imgsource);
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        stream.Close();
                        stream.Dispose();
                        iconImage.Source = bitmap;
                        bitmap.StreamSource.Close();
                    }
                    catch (Exception ex)
                    {
                        logService.WriteToLogLevel("Error reading Image file while creating shortcut icon " + imgsource + "\n" + ex.Message, LogLevelEnum.Error);
                        MessageBox.Show(this, ex.Message);
                    }
                    #endregion


                    //add to stackpanel
                    sp.Children.Add(iconImage);

                    //add stackpanel to button content
                    iconButton.Content = sp;

                    //add iconbutton to toolbar
                    dialogtoolbar.Items.Add(iconButton);
                }
            }
        }

        //Adds all menus and submenu items. But not history or output menu.
        void dashBoardService_AddDashBoardItem(object sender, DashBoardEventArgs e)
        {
            DashBoardItem item = e.DashBoardItem;
            AddToolbarIcon(item);

        }


        #region IOutputWindow Members

        string _windowname;
        public string WindowName
        {
            get { return _windowname; }
            set { _windowname = value; }
        }

        bool tooutputwindow = true;//send this output to output window. 10Aug2012
        public bool ToOutputWindow
        {
            get { return tooutputwindow; }
            set { tooutputwindow = value; }
        }

        bool todiskfile = false;//send this output to disk file. 10Aug2012
        public bool ToDiskFile
        {
            get { return todiskfile; }
            set { todiskfile = value; }
        }
        /// <summary>
        /// // Newly generated analysis is being added to ouput window
        /// </summary>
        /// <param name="analysisdata"></param>
      
        public void AddAnalyis(AnalyticsData analysisdata)
        {
            // Create the first part of the sentence.
            // Create bolded text.
            ICommandAnalyser analyser = CommandAnalyserFactory.GetClientAnalyser(analysisdata);
            CommandOutput output = analyser.Decode(analysisdata);
            output.NameOfAnalysis = analysisdata.AnalysisType;//For Parent Node name 02Aug2012
            if (analysisdata.AnalysisType != null && (analysisdata.AnalysisType.Contains("BSkyFormat") ||analysisdata.AnalysisType.Contains("bskyfrmtobj") ) )
            {
                output.SelectedForDump = analysisdata.SelectedForDump;
                AppendToSyntaxEditorSessionList(output);//18Nov2013
                return;
            }
            if (IsSyntaxSession())//07Nov2014 this can be merged with above 'if'.
            {
                output.SelectedForDump = analysisdata.SelectedForDump;
                AppendToSyntaxEditorSessionList(output);
                return;
            }
            //10Jan2013 output.SelectedForDump = false;/// by default dump no analysis
            ////save output for future for dumping the outputwindow. So that we dont have to regenerate.
            analysisdata.Output = output;//30May2012

            double extraspaceinbeginning = 0;
            if (mypanel.Children.Count > 0)//if its not the first item on panel
                extraspaceinbeginning = 10;

            if (ToOutputWindow)
            {
                foreach (DependencyObject obj in output)
                {
                    FrameworkElement element = obj as FrameworkElement;
                    element.Margin = new Thickness(10, 2+extraspaceinbeginning, 0, 2);
                    mypanel.Children.Add(element);
                    extraspaceinbeginning = 0;
                }
                PopulateTree(output);
                outputDataList.Add(analysisdata);
            }
            if (ToDiskFile) // directly dumping the results from syntax editor.(For oneSMT and crossTab)
            {
                //This will save flexgrid for one sample or crosstab.
                //As one sample or crosstab are generated here
                SynEdtDataList.Add(analysisdata);
            }
            BringOnTop();

        }

        //18Nov2013
        public void AppendToSyntaxEditorSessionList(CommandOutput co)
        {
            //Launch Syntax Editor window with command pasted /// 29Jan2013
            MainWindow mwindow = LifetimeService.Instance.Container.Resolve<MainWindow>();
            ////// Start Syntax Editor  //////
            SyntaxEditorWindow sewindow = LifetimeService.Instance.Container.Resolve<SyntaxEditorWindow>();
            sewindow.AddToSession(co);
        }

        //07Nov2014 Is there anything in sessionlist
        public bool IsSyntaxSession()
        {
            SyntaxEditorWindow sewindow = LifetimeService.Instance.Container.Resolve<SyntaxEditorWindow>();
            if (sewindow.SesssionListItemCount > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Old analysis loaded from .bso file to output window
        /// </summary>
        /// <param name="fullpathfilename"></param>
        public void AddAnalyisFromFile(string fullpathfilename)
        {
            FrameworkElement lastElement = null;
            AnalyticsData analysisdata = null;
            List<SessionOutput> allAnalysis = null;
            BSkyOutputGenerator bsog = new BSkyOutputGenerator();
            allAnalysis = bsog.GenerateOutput(fullpathfilename);
            if (allAnalysis == null)
            {
                return;
            }
            foreach(SessionOutput so in allAnalysis)
            {
                bool isRSession = so.isRSessionOutput;
                if (isRSession)
                {
                    SessionItem = new TreeViewItem();//15Nov2013
                    SessionItem.Header = so.NameOfSession;// (sessionheader != null && sessionheader.Length > 0) ? sessionheader : "R-Session";//18Nov2013 cb;// 
                    SessionItem.IsExpanded = true;
                }

                double extraspaceinbeginning = 0;
                if (mypanel.Children.Count > 0)//if its not the first item on panel
                    extraspaceinbeginning = 10;
                foreach (CommandOutput co in so)
                {
                    analysisdata = new AnalyticsData();//blank entry. There is no open dataset for old/saved ouput
                    analysisdata.Output = co;//saving reference. so that whole outputwindo can be saved again
                    analysisdata.AnalysisType = co.NameOfAnalysis;//For Parent Node name 02Aug2012


                    foreach (DependencyObject obj in co)
                    {
                        FrameworkElement element = obj as FrameworkElement;
                        element.Margin = new Thickness(10, 2+extraspaceinbeginning, 0, 2); ;
                        mypanel.Children.Add(element);
                        extraspaceinbeginning = 0;
                        lastElement = element;
                    }
                    PopulateTree(co, isRSession);
                    outputDataList.Add(analysisdata);
                }
                if (isRSession)
                    NavTree.Items.Add(SessionItem);//15Nov2013
            }

            if (lastElement != null)
                lastElement.BringIntoView();
            BringOnTop();
        }

        //single analysis output from syn edt
        public void AddAnalyisFromSyntaxEditor(CommandOutput lst, string title="")
        {
            AnalyticsData analysisdata = null;
            SessionOutput allanalysis = new SessionOutput();//15Nov2013
            allanalysis.isRSessionOutput = true;//26Nov2013
            allanalysis.Add(lst); //add to all analysis list
            allanalysis.NameOfSession = title;
            allanalysis.isRSessionOutput = true;
            AddSynEdtSessionOutput(allanalysis);

            BringOnTop();
        }

        TreeViewItem SessionItem;//15Nov2013
        //multiple analysis output from syn edt
        public void AddSynEdtSessionOutput(SessionOutput allanalysis)
        {
            AnalyticsData analysisdata = null;
            if (allanalysis.isRSessionOutput)
            {
                SessionItem = new TreeViewItem();//15Nov2013
                SessionItem.Header = (allanalysis.NameOfSession != null && allanalysis.NameOfSession.Length > 0) ? allanalysis.NameOfSession : "R-Session";//18Nov2013 cb;// 
                SessionItem.IsExpanded = true;

                analysisdata = new AnalyticsData();
                analysisdata.SessionOutput = allanalysis;//saving reference. so that whole outputwindo can be saved 
                analysisdata.AnalysisType = allanalysis.NameOfSession;//For Grand-Parent Node name
            }
            
            if (allanalysis == null)// || !last)
            {
                return;
            }
            double extraspaceinbeginning = 0;
            if (mypanel.Children.Count > 0)//if its not the first item on panel
                extraspaceinbeginning = 10;
            foreach (CommandOutput co in allanalysis)
            {
                if (ToOutputWindow) /// sending output to OutputWindow
                {
                    //Original c1flexgrid logic.
                    foreach (DependencyObject obj in co)
                    {
                        FrameworkElement element = obj as FrameworkElement;
                        element.Margin = new Thickness(10, 2 + extraspaceinbeginning, 0, 2); ;
                        mypanel.Children.Add(element);
                        extraspaceinbeginning = 0;
                    }
                    PopulateTree(co, allanalysis.isRSessionOutput);
                }

            }
            if (ToOutputWindow)
            {
                    //if u dont want to send output to window, uncomment following
                    // and comment similar line at the end of this method
                    outputDataList.Add(analysisdata);
            }
            if (ToDiskFile) // directly dumping the results from syntax editor
            {
                //This will not save flexgrid for one sample or crosstab.
                //As these(one sample or crosstab) are generated in AddAnalyis (from Syn Editor)
                SynEdtDataList.Add(analysisdata);
            }
            ///outputDataList.Add(analysisdata);///for sending output to outwindow in all cases.

            if (allanalysis.isRSessionOutput)
                NavTree.Items.Add(SessionItem);//15Nov2013
            BringLastVisibleLeafIntoView(SessionItem);//15Jan2015 //scroll to deepest but visible leaf.
            BringOnTop();
        }

        //21Oct2014 
        //SessionItems are scanned for the first deepest child and that element is brought to view. 
        //Basically fist item of analysis in scrolled(in outputwindow) into the view
        private void BringFirstLeafIntoView(TreeViewItem t)
        {
            //(((SessionItem.Items.GetItemAt(0) as TreeViewItem).Items.GetItemAt(0) as TreeViewItem).Tag as FrameworkElement).BringIntoView();
            if (t.Tag == null && t.Items.Count > 0)
            {
                BringFirstLeafIntoView(t.Items.GetItemAt(0) as TreeViewItem);
            }
            else
            {
                (t.Tag as FrameworkElement).BringIntoView();
            }
        }

        //08Dec2014 
        //SessionItems are scanned for the last child and that element is brought into view. 
        //Basically last item of analysis is scrolled(in outputwindow) into the view.
        //Issue with this logic was : if deepest leaf that is supposed to be brought to view area is 
        //unchecked(for visibility on right pane of output window), ie is non-visible then scroller goes to the top because
        //it couldn't bring invisible item in viewable area as item is collapsed or hidden
        //New method that takes care of this problem is BringLastVisibleLeafIntoView
        private void BringLastLeafIntoView_old(TreeViewItem t)
        {
            if (t.Tag == null && t.Items.Count > 0)
            {
                int lastitem = t.Items.Count;
                TreeViewItem temptvi = t.Items.GetItemAt(lastitem - 1) as TreeViewItem;
                BringLastLeafIntoView_old(temptvi);//go one level deeper and to the last child
            }
            else
            {
                (t.Tag as FrameworkElement).BringIntoView();
                t.BringIntoView();//scroll left tree to latest item in tree.(deepest child node at lowest level)
            }
        }


        //15Jan2015
        //If deepest leaf (TreeViewItem) is set to invisible (Notes and Error/Warning controls by default set to 'collapsed' or 'hidden') 
        // then from this deepest child we move upwards till visible leaf (control like AUParagraph, BSkyNotes, AUGrid, BSkyGraphic)
        // is found under current parent node. If non is found, we move up and consider another parent with all its child nodes.
        //In case we do not find any visible leaf nodes in SessionItem then we do not try to set BringIntoView at all, 
        // and there will be no output for such SessionItem on right pane in output window, so no scrolling is needed.
        private bool BringLastVisibleLeafIntoView(TreeViewItem t)
        {
            bool found=false;
            if (t.Tag == null && t.Items.Count > 0)// non leaf node
            {
                int childcount = t.Items.Count;
                int indexoflastchild = childcount - 1;//zero based index
                do
                {
                    if (indexoflastchild >= 0)
                    {
                        found = BringLastVisibleLeafIntoView(t.Items.GetItemAt(indexoflastchild) as TreeViewItem);
                        indexoflastchild--;
                    }
                    else
                    {
                        break; // no more children, break out of loop and go one level up.
                    }

                } while (!found);//loop current siblings until visible child is found

            }
            else //leaf node
            {
                if ((t.Tag as IAUControl).BSkyControlVisibility == System.Windows.Visibility.Visible)//leaf is set to visible
                {
                    (t.Tag as FrameworkElement).BringIntoView();
                    t.BringIntoView();//scroll left tree to latest item in tree.(deepest child node at lowest level)
                    found = true;//leaf found and set for scroll into view.
                }
                else // else go to sibling leaf which is above this 't'
                {
                    found = false;
                    return found;
                }
            }
            return found;
        }


        public void HardCodedGrid()
        {
            int nrows = 1400; //max 2,147,483,647 for int but not for string[] (ie data)
            C1FlexGrid c1fgrid = new C1FlexGrid();
            List<Employee> emplist = new List<Employee>();
            Employee tmp;
            //creating data
            for (int r = 0; r < nrows; r++)
            {
                tmp = new Employee() { name = "Name" + r, age = r, city = "City" + r };
                emplist.Add(tmp);
            }


            if (emplist != null && emplist.Count > 0)
            {
                c1fgrid.ItemsSource = emplist;
            }

            //Adding C1FlexGrid to stackpanel in output window
            mypanel.Children.Add(c1fgrid);

        }
        
        public void AddMessage(string title, string commandoroutput, bool isCommand=true) // isCommand tells whether commandorouput is command or output
        {
            // Set custom message
            CommandOutput co = new CommandOutput();
            co.NameOfAnalysis = title;
            co.IsFromSyntaxEditor = false;


            string rcommcol = confService.GetConfigValueForKey("errorcol");//23nov2012
            byte red = byte.Parse(rcommcol.Substring(3, 2), NumberStyles.HexNumber);
            byte green = byte.Parse(rcommcol.Substring(5, 2), NumberStyles.HexNumber);
            byte blue = byte.Parse(rcommcol.Substring(7, 2), NumberStyles.HexNumber);
            Color c = Color.FromArgb(255, red, green, blue);

            
            AUParagraph aup = new AUParagraph();
            aup.Text = title;
            aup.ControlType = "Header";
            aup.FontSize = BSkyStyler.BSkyConstants.HEADER_FONTSIZE;//10Nov2014;
            aup.FontWeight = FontWeights.DemiBold;
            aup.textcolor = new SolidColorBrush(c);//Colors.Blue); //SlateBlue //DogerBlue
            co.Add(aup);

            AUParagraph aup2 = new AUParagraph();
            aup.FontSize = BSkyStyler.BSkyConstants.TEXT_FONTSIZE;//10Nov2014
            aup2.Text = commandoroutput;
            aup2.ControlType = isCommand ? "Command" : "Output";
            co.Add(aup2);
            ///send output to output window//
            if (co.Count > 0)
                this.AddAnalyisFromSyntaxEditor(co, title);/// send to output 
            BringOnTop();
        }
       
        #region Read all Analysis in output.

        /// <summary>
        /// if extratags is true that means we need to dump current output for later viewing in outputviewer
        /// false: means we want to open it in html viewer(IE) or csv (in Excel)
        /// ff: is format either C1.WPF.FlexGrid.FileFormat.Html or C1.WPF.FlexGrid.FileFormat.Csv
        /// ff must be Html if dumping output for outputviewer, otherwise viewer will not work
        /// </summary>
        /// <param name="extratags"></param>
        /// 
        UTF8Encoding uniEncoding = new UTF8Encoding();
        public void DumpAllAnalyisOuput(string fullpathzipcsvhtmfilename, C1.WPF.FlexGrid.FileFormat ff, bool extratags)
        {
            string newlinechar = (ff == C1.WPF.FlexGrid.FileFormat.Html && !extratags) ? " <br> " : "\r\n";
            string tabchar = (ff == C1.WPF.FlexGrid.FileFormat.Html) ? " &nbsp; ": "\t";

            imgnamecounter = 0;//11Sep2012
            List<string> filelist = new List<string>();//12Sep2012
            ObservableCollection<AnalyticsData> DataList = null;
            if (SynEdtDataList.Count > 0)
            {
                DataList = SynEdtDataList;
            }
            else
            {
                DataList = outputDataList;
            }
            ///// Creating filename ////
            string filePath = Path.GetDirectoryName(fullpathzipcsvhtmfilename);
            string fileExtension = Path.GetExtension(fullpathzipcsvhtmfilename);
            if (fileExtension.Equals(".bsoz"))
            {
                fileExtension = ".bso";
            }

            string fileNamewithoutExt = Path.GetFileNameWithoutExtension(fullpathzipcsvhtmfilename);
            string fullpathbsocsvhtmfilename = Path.Combine(filePath, fileNamewithoutExt+fileExtension);
            filelist.Add(fileNamewithoutExt + fileExtension);//myout.bso
            ////// root tag/////
            bool savebskytag = extratags;
            FileStream fileStream = new FileStream(fullpathbsocsvhtmfilename, FileMode.Append);
            bool fileExists = File.Exists(fullpathbsocsvhtmfilename);
            string tempString = string.Empty;
            bool oneormorechecked = false;
            ////opening tag/////
            tempString = "<bskyoutput>" + newlinechar;
            if (savebskytag)
                fileStream.Write(uniEncoding.GetBytes(tempString),
                                0, uniEncoding.GetByteCount(tempString));
            /////
            if (ff == FileFormat.Html && !extratags)//21May2015
            {
                string styl = "<style>table td, table th { border: 1px solid #666;} table{border: 1px solid #666;} body > span{ line-height: 30px;} </style>" ;
                tempString = "<!doctype html><html><head><title>" + fullpathbsocsvhtmfilename + " - BlueSky Output</title>" + styl + "</head> <body>";
                fileStream.Write(uniEncoding.GetBytes(tempString),
                                    0, uniEncoding.GetByteCount(tempString));
            }
            /////

            //////// looping thru all analysis one by one //////
            foreach (AnalyticsData analysisdata in DataList)
            {
                CommandOutput output = analysisdata.Output;// getting refrence of already generated objects.
                SessionOutput sessionoutput = analysisdata.SessionOutput;//27Nov2013 if there is session output
                if(output!=null)
                    output.NameOfAnalysis = analysisdata.AnalysisType;//For Parent Node name 02Aug2012
                if(sessionoutput!=null)
                    sessionoutput.NameOfSession = analysisdata.AnalysisType;

                this.ToDiskFile = false;//resetting it back.10Aug2012//Not sure if needed.

                /////// dumping output //if chkbx based dumping then use commented condition///
                if(output!=null)
                {
                    ////opening tag/////
                    tempString = newlinechar + "<sessoutput Header= \"\"   isRsession = \"false\">" + newlinechar;
                    //UnicodeEncoding uniEncoding = new UnicodeEncoding();
                    if (savebskytag)
                        fileStream.Write(uniEncoding.GetBytes(tempString),
                                        0, uniEncoding.GetByteCount(tempString));

                    ExportOutput(output, ff, fileStream, extratags, filelist);
                    oneormorechecked = true;
                    ////closing tag/////
                    tempString = newlinechar + "</sessoutput>" + newlinechar;
                    if (savebskytag)
                        fileStream.Write(uniEncoding.GetBytes(tempString),
                                        0, uniEncoding.GetByteCount(tempString));


                }
                else if (sessionoutput != null)
                {

                    ////opening tag/////
                    tempString = newlinechar + "<sessoutput Header= \"" + sessionoutput.NameOfSession + "\"  isRsession = \"" + sessionoutput.isRSessionOutput + "\">" + newlinechar;
                    //UnicodeEncoding uniEncoding = new UnicodeEncoding();
                    if (savebskytag)
                        fileStream.Write(uniEncoding.GetBytes(tempString),
                                        0, uniEncoding.GetByteCount(tempString));
                    foreach (CommandOutput cout in sessionoutput)
                    {
                        ExportOutput(cout, ff, fileStream, extratags, filelist, true);
                        oneormorechecked = true;
                    }
                    ////closing tag/////
                    tempString =newlinechar+ "</sessoutput>" + newlinechar;
                    if (savebskytag)
                        fileStream.Write(uniEncoding.GetBytes(tempString),
                                        0, uniEncoding.GetByteCount(tempString));
                }

            }

            ////closing tag/////
            tempString = "</bskyoutput>" + newlinechar;
            if (savebskytag)
                fileStream.Write(uniEncoding.GetBytes(tempString),
                                0, uniEncoding.GetByteCount(tempString));

            /////
            if (ff == FileFormat.Html && !extratags)//21May2015
            {
                tempString = "</body></html>";
                fileStream.Write(uniEncoding.GetBytes(tempString),
                                    0, uniEncoding.GetByteCount(tempString));
            }
            /////

            fileStream.Close();
            SynEdtDataList.Clear();//Clearing local list after dumping. 08Aug2012

            //// Now all files are ready to be zipped in one, if user chose .bsoz format ////
            if(extratags)
                CreateBSkyZipOutput(fullpathzipcsvhtmfilename, filelist);
  
        }

        #endregion

        #region Save as CSV or HTML.

        private void ExportOutput(CommandOutput output, C1.WPF.FlexGrid.FileFormat ff, FileStream fileStream, bool extratags, List<string> filelist, bool issessionout=false)//csv of excel
        {
            //// CSV format can be used to open in Excel for making changes
            //// HTML format with extratags false can be used to open in internet browser, for viewing
            //// HTML format with extratags true can be used to open in standalone output viewer
            if (output.NameOfAnalysis == null)
                output.NameOfAnalysis = string.Empty;

            string newlinechar = (ff == C1.WPF.FlexGrid.FileFormat.Html && !extratags) ? " <br> " : " \r\n ";
            string tabchar = (ff == C1.WPF.FlexGrid.FileFormat.Html) ? " &nbsp; " : " \t ";

            ////for export to excel///B/ 
            string tempString = "<bskyanalysis>" + newlinechar + "<analysisname> " + output.NameOfAnalysis.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">","&gt;") + " </analysisname>" + newlinechar;


            //////Writing header tag for each analysis//////
            if (extratags)
                fileStream.Write(uniEncoding.GetBytes(tempString),
                                    0, uniEncoding.GetByteCount(tempString));

            foreach (DependencyObject obj in output)
            {
                FrameworkElement element = obj as FrameworkElement;
                //31Aug2012 AUXGrid xgrid = element as AUXGrid; 
                if ((element as AUParagraph) != null)
                {
                    AUParagraph aup = element as AUParagraph;
                    string ctrltype = (aup.ControlType != null) ? aup.ControlType.Replace("\"", "&quot;").Replace("\'", "&apos;") : "-";//09Jul2013
                    if (aup.Text != null)///// <aup> means AUParagraph
                    {
                        //controltype
                        string CONTROLTYPE = (" controltype = \"" + ctrltype + "\" ").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

                        //saving color also//
                        SolidColorBrush scb = (SolidColorBrush)aup.textcolor;
                        string hexcol = scb.Color.ToString();
                        if (hexcol == null) hexcol = "#FF000000";
                        string TEXTCOL = (" textcolor = \"" + hexcol + "\" ").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

                        //saving font size//
                        string fontsize = Convert.ToString(aup.FontSize);
                        if (fontsize == null) fontsize = "14";
                        string FONTSIZE = (" fontsize = \"" + fontsize + "\" ").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

                        //saving font weight//
                        FontWeight fw = aup.FontWeight;
                        FontWeightConverter fwc = new FontWeightConverter();
                        string fontwt = fwc.ConvertToString(fw);
                        if (fontwt == null) fontwt = "{Normal}";
                        string FONTWT = (" fontweight = \"" + fontwt + "\" ").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
                        if (ff == FileFormat.Html && !extratags)
                        {
                            FONTWT = fontwt.Trim().Equals("{Normal}") ? "normal" : "bold";
                        }
                        //text
                        string TEXT = (aup.Text).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("\'", "&apos;");
                        if (ff == FileFormat.Html && !extratags)
                            TEXT = "<span style=\"color:" + hexcol + "; font-weight:" + FONTWT + "; font-size:" + fontsize + "px;\">" + TEXT + "</span>";
                        tempString = (extratags) ? " <aup " + CONTROLTYPE + TEXTCOL + FONTSIZE + FONTWT + "> " + TEXT + " </aup>" + newlinechar : TEXT + newlinechar;
                    }
                    byte[] arr = uniEncoding.GetBytes(tempString);
                    fileStream.Write(arr, 0, uniEncoding.GetByteCount(tempString));
                }
                else if ((element as AUXGrid) != null)
                {
                    AUXGrid xgrid = element as AUXGrid; //31Aug2012
                    //////opening auxgrid////<auxgrid>
                    tempString = "<auxgrid>" + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));

                    ////////// Printing Header //////////  <fgheader> means flexgrid header
                    string header = (extratags) ? newlinechar + "<fgheader> " + xgrid.Header.Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + " </fgheader>" + newlinechar : xgrid.Header.Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");// +newlinechar;
                    string htmlfgheader = "<p><span style=\"color:" + "#FF000000" + "; font-weight:" + "bold" + "; font-size:" + 16 + "px;\">" + header + "</span>";
                    if (ff == FileFormat.Html && !extratags)//23May2015
                    {
                        fileStream.Write(uniEncoding.GetBytes(htmlfgheader),
                                0, uniEncoding.GetByteCount(htmlfgheader));
                    }
                    else
                    {
                        fileStream.Write(uniEncoding.GetBytes(header),
                                0, uniEncoding.GetByteCount(header));
                    }

                    //////////////// Printing Errors ///////////
                    if (xgrid.Metadata != null)//// <errhd> means error heading
                    {
                        header = (extratags) ? "<errhd> Errors/Warnings: </errhd>" + newlinechar : " Errors/Warnings: " + newlinechar; ////error header
                        fileStream.Write(uniEncoding.GetBytes(header),
                        0, uniEncoding.GetByteCount(header));

                        AUParagraph paragraph = new AUParagraph();
                        foreach (KeyValuePair<char, string> keyval in xgrid.Metadata)
                        {
                            paragraph.Text = keyval.Key.ToString() + ":" + keyval.Value; ///// <errm> means error/warning message
                            header = (extratags) ? "<errm> \"" + paragraph.Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "\" </errm> " + newlinechar : "\"" + paragraph.Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "\" " + newlinechar;
                            fileStream.Write(uniEncoding.GetBytes(header),
                            0, uniEncoding.GetByteCount(header));
                        }
                    }

                    //////// Printing  Grid ////////////
                    //fileStream.Flush(true);//10Aug2012

                    AUGrid grid = xgrid.Grid;
                    if (ff == FileFormat.Html && !extratags) //if save as HTML is choosen
                    {
                        ////getting FlexGrid HTML and modifying it then saving to output HTML file////
                        using (var fStream = new MemoryStream())
                        {
                            grid.Save(fStream, C1.WPF.FlexGrid.FileFormat.Html);//change file format here for csv to any other
                            byte[] fgdatarr = new byte[fStream.Length];
                            fStream.Position = 0;//3 or 4
                            fStream.Read(fgdatarr, 0, fgdatarr.Length);
                            string fgstrdata = System.Text.Encoding.UTF8.GetString(fgdatarr);
                            int idxhtml = fgstrdata.IndexOf("<html>");
                            fgstrdata = fgstrdata.Substring(idxhtml).Replace("<html>", "&nbsp;").Replace("<head>", "&nbsp;").Replace("<body>", "&nbsp;").Replace("</body>", "&nbsp;").Replace("</html>", "&nbsp;");
                            fileStream.Write(uniEncoding.GetBytes(fgstrdata), 0, uniEncoding.GetByteCount(fgstrdata));

                            fStream.Close();
                        }
                    }
                    else
                    {

                        grid.Save(fileStream, ff);//change file format here for csv to any other
                    }
                    fileStream.WriteByte(13);
                    fileStream.WriteByte(10);

                    /////////////////Printing Footer  ///////////////
                    if (xgrid.FootNotes != null)
                    {
                        if (xgrid.FootNotes.Count > 0)
                        {
                            header = (extratags) ? "<ftheader> Footnotes </ftheader>" + newlinechar : "Footnotes " + newlinechar; ////error header
                            fileStream.Write(uniEncoding.GetBytes(header),
                            0, uniEncoding.GetByteCount(header));
                        }
                        AUParagraph footnote = new AUParagraph();
                        foreach (KeyValuePair<char, string> keyval in xgrid.FootNotes)
                        {
                            footnote.Text = keyval.Key.ToString() + ":" + keyval.Value;
                            header = (extratags) ? "<footermsg> \"" + footnote.Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "\" </footermsg> " + newlinechar : "\"" + footnote.Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "\" " + newlinechar;
                            fileStream.Write(uniEncoding.GetBytes(header),
                                    0, uniEncoding.GetByteCount(header));
                        }
                    }
                    /////closing auxgrid////<auxgrid>
                    tempString = " </auxgrid> " + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));
                }
                else if ((element as BSkyGraphicControl) != null)//Graphics 31Aug2012
                {
                    BSkyGraphicControl bsgc = element as BSkyGraphicControl;

                    //Create image filename
                    string imgfilename = fileStream.Name + bsgc.ImageName + ".png";
                    filelist.Add(Path.GetFileNameWithoutExtension(imgfilename) + ".png");//*.png
                    imgnamecounter++;

                    //Saving Image separately
                    BSkyGraphicControlToImageFile(bsgc, imgfilename);

                    //10Nov2014
                    string imgtag = "<img src=\"" + imgfilename + "\" alt=\"Graphic Here\" >";//style=\"width:304px;height:228px\"
                    //saving tag in .BSO file
                    string grpcomm = (extratags) ? "<graphic>" + imgfilename + "</graphic>" + newlinechar : imgtag + newlinechar; ////error header
                    fileStream.Write(uniEncoding.GetBytes(grpcomm),
                    0, uniEncoding.GetByteCount(grpcomm));

                }
                else if((element as BSkyNotes) != null && extratags) // Notes Control 05Nov2012. //21May2015 extratags is added to save NOTES only when bsoz file is created and not for other formats.
                {
                    BSkyNotes bsn = element as BSkyNotes;
                    string ctrltype = (bsn.ControlType != null) ? bsn.ControlType : "-";//09Jul2013
                    string colltext = (bsn.CollapsedText != null) ? bsn.CollapsedText : "";//09Jul2013
                    int disprowindex = bsn.ShowRow_Index;
                    uint splitposi = bsn.NotesSplitPosition;
                    string[,] notesdata = bsn.NotesData;
                    string heading = bsn.HearderText;
                    string collapsedText = bsn.SummaryText;
                    ///Opening tag///
                    tempString = "<bskynotes> " + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));

                    /// set control type display ///
                    tempString = "<controltype>" + ctrltype.ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</controltype> " + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));

                    /// set collapse text display ///
                    tempString = "<collapsetext>" + colltext.ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</collapsetext> " + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));

                    /// set row index to display ///
                    tempString = "<showrow>" + disprowindex.ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</showrow> " + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));

                    /// set split index for drawing vertical line in middle ///
                    tempString = "<splitposi>" + splitposi.ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</splitposi> " + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));

                    /// set row index to display /////written to CSV also
                    tempString = (extratags) ? "<notesheading>" + heading.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</notesheading> " + newlinechar : heading + newlinechar;
                    //if (extratags) 
                        fileStream.Write(uniEncoding.GetBytes(tempString),
                                            0, uniEncoding.GetByteCount(tempString)); 
                    ///// Notes Data /////not written to CSV but written to HTML and BSO
                        if (ff == C1.WPF.FlexGrid.FileFormat.Html)//04Feb2013  only if condition add. Body code is old
                        {
                            string celldata = string.Empty;
                            for (int row = 0; row < notesdata.GetLength(0); row++)
                            {
                                //start row//
                                tempString = "<notesrow> " + newlinechar;
                                fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                                    0, uniEncoding.GetByteCount(tempString.Trim()));
                                ///columns inside each row ///
                                for (int col = 0; col < notesdata.GetLength(1); col++)
                                {
                                    celldata = notesdata[row, col];
                                    if (celldata != null && celldata.Trim().Length > 0)
                                    {
                                        tempString = (extratags) ? " <notescol> " + celldata.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + " </notescol> " + newlinechar : celldata.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + " &nbsp;&nbsp;&nbsp;&nbsp; ";
                                    }
                                    else
                                    {
                                        tempString = "<notescol> </notescol>" + newlinechar;
                                    }
                                    fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                        0, uniEncoding.GetByteCount(tempString.Trim()));
                                }
                                //end row//
                                tempString = "</notesrow> " + newlinechar;
                                fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                                    0, uniEncoding.GetByteCount(tempString.Trim()));
                            }
                        }
                        else// write collapsed text of Notes in CSV //04Feb2013
                        {
                            tempString = collapsedText.Trim()+newlinechar; /// \n first must
                            fileStream.Write(uniEncoding.GetBytes(tempString),
                                                0, uniEncoding.GetByteCount(tempString));
                        }
                    ///Closing tag///
                    tempString = "</bskynotes> " + newlinechar;
                    if (extratags)
                        fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                            0, uniEncoding.GetByteCount(tempString.Trim()));
                }
            }

            /////ending tag for  each analysis//////
            tempString = " </bskyanalysis> " + newlinechar;
            if (extratags)
                fileStream.Write(uniEncoding.GetBytes(tempString.Trim()),
                                    0, uniEncoding.GetByteCount(tempString.Trim()));
        }

        private void BSkyGraphicControlToImageFile(BSkyGraphicControl bsgc, string fullpathimgfilename)
        {
            Image myImage = new System.Windows.Controls.Image();
            myImage.Source = bsgc.BSkyImageSource;
            //BitmapImage bi = im.Source;

            //System.Windows.Controls.Image myImage = ((Image)obj);
            System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            bitmapImage = ((System.Windows.Media.Imaging.BitmapImage)myImage.Source);
            System.Windows.Media.Imaging.PngBitmapEncoder pngBitmapEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            System.IO.FileStream stream = new System.IO.FileStream(fullpathimgfilename, FileMode.Create);

            pngBitmapEncoder.Interlace = PngInterlaceOption.On;
            pngBitmapEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage));
            pngBitmapEncoder.Save(stream);
            stream.Flush();
            stream.Close();
        }
        #endregion

        # region Zip output files
        private void CreateBSkyZipOutput(string fullpathzipfilename, List<string> filelist)
        { 
            string filename = fullpathzipfilename;
            if (true)//files exists those we need to zip
            {
                string fileNamewithoutExt = Path.GetFileNameWithoutExtension(filename);
                string filePath = Path.GetDirectoryName(filename);
                string zipFileName = fileNamewithoutExt;
                zipFileName = Path.Combine(filePath, zipFileName + ".bsoz");

                ///// Creating/Overwriting zip file with fresh entries(ie. all entries in filelist)///
                ZipFile zf = ZipFile.Create(zipFileName);
                zf.BeginUpdate();
                foreach (string fname in filelist)
                {
                    if(File.Exists(Path.Combine(filePath, fname)))
                    zf.Add(Path.Combine(filePath, fname), fname);
                }
                zf.CommitUpdate();
                zf.Close();

                //17Mar2015
                //////// Deleting after zipping //////
                try
                {
                    foreach (string fname in filelist)//remove files those are already begin zipped
                    {
                        File.Delete(Path.Combine(filePath, fname));
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Cleaning temporary file failed. Not Authorized.");
                }
            }
        }
        #endregion
        /// <summary>
        /// Creating Treeview
        /// </summary>
        /// <param name="output"></param>
        private void PopulateTree(CommandOutput output, bool synedtsession=false)
        {
            string treenocharscount = confService.GetConfigValueForKey("nooftreechars");//16Dec2013
            int openbracketindex, max;
            string analysisName = string.Empty;
            
            if(output.NameOfAnalysis != null && output.NameOfAnalysis.Trim().Length > 0) // For shortening left tree parent node name.
            {
                openbracketindex = output.NameOfAnalysis.Contains("(") ? output.NameOfAnalysis.IndexOf('(') : output.NameOfAnalysis.Length;
                analysisName = output.NameOfAnalysis.Substring(0, openbracketindex);//18Nov2013 (0, max);
                if (output.NameOfAnalysis.Contains("BSkyFormat("))//it is output
                    analysisName = "BSkyFormat-Output";
            }
            else
            {
                analysisName = "Output";
            }

            //// Main logic to populate tree ////
            TreeViewItem MainItem = new TreeViewItem(); 
            MainItem.Header = analysisName;
            MainItem.IsExpanded = true;
            List<string> Headers = new List<string>();
            if (MainItem.Header.ToString().Contains("Execution Started"))
            {
                MainItem.Background = Brushes.LawnGreen;
            }
            if (MainItem.Header.ToString().Contains("Execution Ended"))
                MainItem.Background = Brushes.SkyBlue;
            //bool setFocus = true;
            
            foreach (DependencyObject obj in output)
            {
                IAUControl control = obj as IAUControl;
                if (control == null) continue;//for non IAUControl
                Headers.Add(control.ControlType);
                TreeViewItem tvi = new TreeViewItem();

                ////Setting common Excel sheet/////
                AUParagraph _aup = obj as AUParagraph;
                if (_aup != null)
                    _aup.MSExcelObj = _MSExcelObj;
                BSkyNotes _note = obj as BSkyNotes;
                if (_note != null)
                    _note.MSExcelObj = _MSExcelObj;
                AUXGrid _aux = obj as AUXGrid;
                if (_aux != null)
                {
                    _aux.MSExcelObj = _MSExcelObj;
                }
                ////23Oct2013. for show hide leaf nodes based on checkbox //
                StackPanel treenodesp = new StackPanel();
                treenodesp.Orientation = Orientation.Horizontal;
                
                    int treenodecharlen;
                    bool result = Int32.TryParse(treenocharscount, out treenodecharlen);
                    if (!result)
                        treenodecharlen = 15;

                    TextBlock nodetb = new TextBlock();
                    nodetb.Tag = control;
                    int maxlen = control.ControlType.Length < treenodecharlen ? control.ControlType.Length : (treenodecharlen);
                    string dots = maxlen <= treenodecharlen ? "..." : "...";
                    nodetb.Text = control.ControlType.Substring(0, maxlen) + dots;
                    nodetb.Margin = new Thickness(1);
                    nodetb.GotFocus+=new RoutedEventHandler(nodetb_GotFocus);
                    nodetb.LostFocus+=new RoutedEventHandler(nodetb_LostFocus);
                    nodetb.ToolTip = "Click to bring the item in the view";

                    CheckBox cbleaf = new CheckBox();
                    cbleaf.Content = "";// control.ControlType;
                    cbleaf.Tag = control;
                    cbleaf.Checked += new RoutedEventHandler(cbleaf_Checked);
                    cbleaf.Unchecked += new RoutedEventHandler(cbleaf_Checked);
                    cbleaf.Visibility = System.Windows.Visibility.Visible;///unhide to see it on output window.
                    cbleaf.ToolTip = "Select/Unselect this node to show/hide in right pane";
                    if (!(control is BSkyNotes))
                        cbleaf.IsChecked = true;

                treenodesp.Children.Add(cbleaf);
                treenodesp.Children.Add(nodetb);

                tvi.Header = treenodesp;// cbleaf;//.Substring(0,openbracketindex);/// Leaf Node Text
                tvi.Tag = control;

                tvi.Selected += new RoutedEventHandler(tvi_Selected);
                tvi.Unselected += new RoutedEventHandler(tvi_Unselected);//29Jan2013
                MainItem.Items.Add(tvi);
            }
            if(synedtsession)
                SessionItem.Items.Add(MainItem);
            else
                NavTree.Items.Add(MainItem);
        }

        /// <summary>
        /// When treeview items are clicked, that item should come in focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void tvi_Selected(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            TreeViewItem tvi = fe as TreeViewItem;
            ((tvi.Header as StackPanel).Children[0] as CheckBox).IsChecked = true;
            FrameworkElement tag = fe.Tag as FrameworkElement;
            IAUControl control = tag as IAUControl;
            string navtreeselcom = confService.GetConfigValueForKey("navtreeselectedcol");//23nov2012
            byte red = byte.Parse(navtreeselcom.Substring(3, 2), NumberStyles.HexNumber);
            byte green = byte.Parse(navtreeselcom.Substring(5, 2), NumberStyles.HexNumber);
            byte blue = byte.Parse(navtreeselcom.Substring(7, 2), NumberStyles.HexNumber);
            Color c = Color.FromArgb(255, red, green, blue);
            control.bordercolor = new SolidColorBrush(c);// (Colors.Gold);//05Jun2013
            tag.BringIntoView(); //treeview leaf node will appear selected as oppose to Focus()
        }

        //29Jan2013
        ///// <summary>
        ///// When treeview items are unselected, that item should set back default background color
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        void tvi_Unselected(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            FrameworkElement tag = fe.Tag as FrameworkElement;
            //scrollviewer.BringIntoView(tag.GetVisualBounds(this));
            IAUControl control = tag as IAUControl;
            ////05Jun2013 control.outerborderthickness = new Thickness(0);
            control.bordercolor = new SolidColorBrush(Colors.Transparent);//05Jun2013
        }

        void cb_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            CommandOutput tag = cb.Tag as CommandOutput;

            if (cb.IsChecked == true)
            {
                tag.SelectedForDump = true; // dump this analysis
            }
            else
            {
                tag.SelectedForDump = false;
            }

            if (cb != null)
            {
                TreeViewItem tvparentnode = (TreeViewItem)cb.Parent;
                StackPanel leafnodesp;
                CheckBox leafcb;
                foreach (TreeViewItem tvi in tvparentnode.Items)
                {
                    leafnodesp = (StackPanel)tvi.Header;
                    leafcb = (CheckBox)leafnodesp.Children[0];
                    leafcb.IsChecked = cb.IsChecked;
                    
                }
            }
        }

        //23Oct2013 for leaf nodes. When you check right panel will show associated item.
        void cbleaf_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;

            if (cb.IsChecked == true)
            {
                (cb.Tag as IAUControl).BSkyControlVisibility = System.Windows.Visibility.Visible;
            }
            else
            {
                (cb.Tag as IAUControl).BSkyControlVisibility = System.Windows.Visibility.Collapsed;
            }

            if (cb != null)
            {
                StackPanel leafsp = (StackPanel)cb.Parent;
                if (leafsp == null) return;
                TreeViewItem tvleafnode = (TreeViewItem)leafsp.Parent;
                if (tvleafnode != null)
                {
                    TreeViewItem tvparentnode = (TreeViewItem)tvleafnode.Parent;
                    if (tvparentnode != null)
                    {
                        StackPanel leafnodesp;
                        CheckBox leafcb;
                        // state of currently checked/unchecked leaf. Now see if all leaf matches to this.
                        // if current checked and all other leafs are checked then parent node should be checked and vice versa.
                        bool ischked = cb.IsChecked == true ? true : false;
                        bool match = true; // all leaf are in matching checked/unchecked state or not
                        foreach (TreeViewItem tvi in tvparentnode.Items)
                        {
                            leafnodesp = (StackPanel)tvi.Header;
                            leafcb = (CheckBox)leafnodesp.Children[0];
                            if (leafcb.IsChecked != ischked)//if at least one mismatch leaf is found. Parent state will not change and will to match to current leaf state.
                            {
                                //ischked = !ischked; //reversed
                                match = false;
                                break;
                            }

                        }
                    }
                }
            }
        }


        //23Oct2013 for leaf nodes. When you check golden border will appear for easy finding
        void nodetb_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            
            (tb.Tag as IAUControl).bordercolor = new SolidColorBrush(Colors.Gold);
            (tb.Tag as UserControl).BringIntoView();
        }

        //23Oct2013 for leaf nodes. When you check another item, the old item having golden border will not have that border anymore.
        void nodetb_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            (tb.Tag as IAUControl).bordercolor = new SolidColorBrush(Colors.Transparent);

        }


        #endregion

        #region Output Window closing/closed events
        /// <summary>
        /// When user clicks on window close button on the right top corner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void outwin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Activate();
            System.Windows.Forms.DialogResult dresult = System.Windows.Forms.MessageBox.Show( ///15Jan2013
                      "Do you want to save the output of "+this.WindowName+"? \nYes : Save all and Close, \nNo : Close without Save, \nCancel: Do not Save/Close this window.",
                      "Save Output?",
                      System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                      System.Windows.Forms.MessageBoxIcon.Question);
            if (dresult == System.Windows.Forms.DialogResult.Yes)//SaveAll and close
            {
                ToggleSelectAllItems(true);// select all items for dumping
                SaveAs();
            }
            if (dresult == System.Windows.Forms.DialogResult.Cancel)//Do not close window. and Do not Save.(for selective save)
            {
                e.Cancel = true;
            }

            #region Syntax Save and Close //06May2015
            //// Also provide save option for syntax section
            if (!e.Cancel)//if window closing is not aborted then ask for syn save
            {
                bool isClose = SynWindow_Closing();
                if (!isClose) //abort close
                {
                    e.Cancel = true;
                }
            }
            #endregion

        }

        /// <summary>
        /// When output window is closed it should be removed from container and its name from output menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void outwin_Closed(object sender, EventArgs e)
        {
                 OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
                 IOutputWindow iow = sender as IOutputWindow;
                 string windowname = iow.WindowName;
                 owc.RemoveOutputWindow(windowname);//Maybe we can also use _windowname. Abv 2 lines not required
        }
        #endregion



        /// <summary>
        /// For opening .bsoz file in  outputwindow. Many files can be opened in one output window.
        /// It will append output of another .bso, if multiple file are being opened
        /// </summary>
        private void open_Click(object sender, RoutedEventArgs e)
        {
            UAMenuCommand uamc = new UAMenuCommand();
            uamc.commandformat = _windowname;//using commandformat as temporary for storing windowname

            OutputOpenCommand osac = new OutputOpenCommand();
            osac.Execute(uamc);
        }

        /// <summary>
        /// To dump the output of current window to a file. (.bso)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dump_Click(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private void SaveAs()
        {
            // Dumping starts ////
            UAMenuCommand uamc = new UAMenuCommand();
            uamc.commandformat = _windowname;//using commandformat as temporary for storing windowname

            OutputSaveAsCommand osac = new OutputSaveAsCommand();
            osac.Execute(uamc);
        }
        
        //Checking if at least one output is selected/checked for dumping 
        public bool IsOneOrMoreSelected()//24Jan2013
        {
            bool oneormoreslected = false;
            foreach (TreeViewItem tvm in NavTree.Items)
            {
                CheckBox cb = null;
                if (tvm.Header is CheckBox)
                {
                    cb = tvm.Header as CheckBox;
                    if (cb.IsChecked == true)
                    {
                        oneormoreslected = true;
                        break;
                    }
                }
            }
            return oneormoreslected;
        }


        //Selecting Deselecting all output
        private void selectall_Click(object sender, RoutedEventArgs e)//24Jan2013
        {
            MenuItem mi = (sender as MenuItem);
            if (NavTree.Items.Count > 0) //if there are items in tree, then only action will be preformed
            {
                bool toggle = (mi.Header as string).Equals("Select All") ? true : false;
                ToggleSelectAllItems(toggle);

                if (toggle)
                {
                    mi.Header = "Deselect All";
                }
                else
                {
                    mi.Header = "Select All";
                }
            }
        }

        /// <summary>
        /// Toggle Select-Deselect All
        /// </summary>
        /// <param name="toggle"></param>
        private void ToggleSelectAllItems(bool toggle) // Toggle=True(select all), Toggle=False(deselectAll)
        {
            foreach (TreeViewItem tvm in NavTree.Items)
            {
                CheckBox cb = null;
                if (tvm.Header is CheckBox)
                {
                    cb = tvm.Header as CheckBox;
                    cb.IsChecked = toggle;
                }
            }        
        }

        /// Bring this window on Top///
        public void BringOnTop()
        {
            //08Aug2013 if new output is generated and this window is in minimized state then it will be restored to normal
            if (this.WindowState == System.Windows.WindowState.Minimized)
                this.WindowState = System.Windows.WindowState.Normal;

            this.Activate();//bring it to front
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        ////Dumping output(from window or syntax editor )
        //private void DumpResults()
        //{

        //}

        #region Syntax Window Events and other related stuff

        string syntitle = "R Command Editor - ";
        bool SEForceClose = false;
        bool Modified = false; //19Feb2013 to track if any modification has been done after last save

        private void SyntaxtInit() //syntax init
        {


        }

        public bool SynEdtForceClose
        {
            get { return SEForceClose; }
            set { SEForceClose = value; }
        }

        private void runButton_Click(object sender, RoutedEventArgs e)
        {
            ////// Start Syntax Editor  //////
            sewindow = LifetimeService.Instance.Container.Resolve<SyntaxEditorWindow>();

            string commands = inputTextbox.SelectedText;//selected text
            if (commands != null && commands.Length > 0)
            {
                //MessageBox.Show(seltext);
            }
            else
            {
                commands = inputTextbox.Text;//All text
                //MessageBox.Show(seltext);
            }
            if (commands.Trim().Length > 0)
            {
                sewindow.RunCommands(commands);
                sewindow.DisplayAllSessionOutput("", this);
            }
        }

        private void browse_Click(object sender, RoutedEventArgs e)
        {
            string FileNameFilter = "Bsky Format, that can be opened in Output Window later (*.bsoz)|*.bsoz|Comma Seperated (*.csv)|*.csv|HTML (*.html)|*.html"; //BSkyOutput
            SaveFileDialog saveasFileDialog = new SaveFileDialog();
            saveasFileDialog.Filter = FileNameFilter;

            bool? output = saveasFileDialog.ShowDialog(Application.Current.MainWindow);
            if (output.HasValue && output.Value)
            {
                try
                {
                    if (File.Exists(saveasFileDialog.FileName))
                    {
                        File.Delete(saveasFileDialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                    logService.WriteToLogLevel(ex.Message, LogLevelEnum.Error);
                    return;
                }

            }
        }

        public void PasteSyntax(string command)//29Jan2013
        {
            int oldlinecount = inputTextbox.LineCount;// -1;
            
            string newlines = (inputTextbox.Text != null && inputTextbox.Text.Trim().Length > 0) ? "\n\n" : string.Empty;
            if (command != null && command.Length > 0)
                inputTextbox.AppendText(newlines + command);// inputTextbox.Text = existingCommands + command;
            int linecount = inputTextbox.LineCount - 1;
            inputTextbox.ScrollToLine(linecount); // linecount is zero based so decremented by 1 in above line


            int pastestartidx = inputTextbox.GetCharacterIndexFromLineIndex(oldlinecount);
            
            inputTextbox.Select(pastestartidx, inputTextbox.Text.Length);
        }

        //New : clears the command area
        private void MenuItemNew_Click(object sender, RoutedEventArgs e)
        {
            if (Modified)//if any modification done to command scripts after last Save
            {
                CloseCurrentScript();
            }
            else
            {
                ResetValues();
            }

        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            SyntaxEditorOpen();
            inputTextbox.Focus();// this.Activate();
        }

        //26May2015
        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            SyntaxEditorSave();
            inputTextbox.Focus();
        }
       
        private void MenuItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SyntaxEditorSaveAs();
            inputTextbox.Focus();// this.Activate();
        }

        private void SyntaxEditorOpen()
        {
            bool isClosed=true;
            if (Modified)//if any modification done to command scripts after last Save
            {
                isClosed = CloseCurrentScript();
            }

            if (isClosed)//if current doc is closed finally then ask for opening another
            {
                const string FileNameFilter = "BSky R scripts, (*.r)|*.r"; //BSkyR
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = FileNameFilter;
                bool? output = openFileDialog.ShowDialog(Application.Current.MainWindow);
                if (output.HasValue && output.Value)
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(openFileDialog.FileName);
                    inputTextbox.Text = file.ReadToEnd();
                    file.Close();
                    currentScriptFname = openFileDialog.FileName;//26May2015
                    recentSyntaxfiles.AddXMLItem(openFileDialog.FileName);//19May2015
                }
                Modified = false;//19Feb2013 Newly loaded script can only be modified after loading finishes.
                SyntaxTitle.Text = syntitle + openFileDialog.FileName; //19Feb2013
            }
        }

        private bool CloseCurrentScript()
        {
            bool isClosed = false;
            //allow user to save changes before opening another command script
            System.Windows.Forms.DialogResult dresult = System.Windows.Forms.MessageBox.Show(
                      "Do you want to save current script?",
                      "Save & Close current command script?",
                      System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                      System.Windows.Forms.MessageBoxIcon.Question);
            if (dresult == System.Windows.Forms.DialogResult.Yes)//Yes Save- and Close
            {
                bool isSaved;
                if (currentScriptFname != null && currentScriptFname.Length > 0)
                    isSaved = SyntaxEditorSave();
                else
                    isSaved = SyntaxEditorSaveAs();

                if (isSaved) // reset values if script saved.
                {
                    ResetValues();
                    isClosed = true;//close current script
                }

            }
            else if (dresult == System.Windows.Forms.DialogResult.No)//No Save- but Close
            {
                ResetValues();
                isClosed = true;
            }
            else//no Save no Close
            {
                //return isClosed;
            }
            return isClosed;
        }

        string currentScriptFname = null;
        private bool SyntaxEditorSaveAs()
        {
            bool isSaved = false; //not saved.
            const string FileNameFilter = "BSky R scripts, (*.r)|*.r"; //BSkyR. Extension is changed to .r
            SaveFileDialog saveasFileDialog = new SaveFileDialog();
            saveasFileDialog.Filter = FileNameFilter;
            bool? output = saveasFileDialog.ShowDialog(Application.Current.MainWindow);
            if (output.HasValue && output.Value)
            {
                currentScriptFname = saveasFileDialog.FileName;//26May2015
                SaveScript(currentScriptFname);
                isSaved = true; //saved
            }
            return isSaved;
        }

        ////26May2015 Saves to current file.
        private bool SyntaxEditorSave()
        {
            bool isSaved = false;//not saved
            if (currentScriptFname == null || currentScriptFname.Trim().Length < 1)
            {
                const string FileNameFilter = "BSky R scripts, (*.r)|*.r"; //BSkyR. Extension is changed to .r
                SaveFileDialog saveasFileDialog = new SaveFileDialog();
                saveasFileDialog.Filter = FileNameFilter;
                bool? output = saveasFileDialog.ShowDialog(Application.Current.MainWindow);
                if (output.HasValue && output.Value)
                {
                    currentScriptFname = saveasFileDialog.FileName;
                    SaveScript(currentScriptFname);
                    isSaved = true;//saved
                }
            }
            else
            {
                SaveScript(currentScriptFname);
                isSaved = true;
            }
            return isSaved;
        }

        ////26May2015 save current script to a file.
        private void SaveScript(string fullpathfilename)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fullpathfilename);
            file.WriteLine(inputTextbox.Text);
            file.Close();

            Modified = false;//19Feb2013 currently saving. So immediately after save there are no new changes/modifications.
            SyntaxTitle.Text = syntitle + currentScriptFname; //19Feb2013
        }

        //26May2015 Reset vars and control values
        private void ResetValues()
        {
            currentScriptFname = string.Empty;//26May2015
            SyntaxTitle.Text = syntitle; //26May2015
            inputTextbox.Text = string.Empty;
            inputTextbox.Focus();// this.Activate();
        }

        //19Feb2013 If anybody edits/changes something in text-area
        private void inputTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentScriptFname == null)
                currentScriptFname = string.Empty;
            Modified = true;
            SyntaxTitle.Text = syntitle+" "+currentScriptFname+" < unsaved script >";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ////// Start Syntax Editor  //////
            SyntaxEditorWindow sewindow = LifetimeService.Instance.Container.Resolve<SyntaxEditorWindow>();
            sewindow.RefreshBothgrids();//16Jul2015 refreshes both the grids
        }

        public void ActivateSyntax()
        {
            inputTextbox.Focus();
        }

        private bool SynWindow_Closing()
        {
                System.Windows.Forms.DialogResult dresult = System.Windows.Forms.DialogResult.OK;
                if (Modified)
                {
                    dresult = System.Windows.Forms.MessageBox.Show(
                              "Do you want to save commands before closing Command Editor?",
                              "Save & Exit Command Editor?",
                              System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                              System.Windows.Forms.MessageBoxIcon.Question);
                    if (dresult == System.Windows.Forms.DialogResult.Cancel)//dont close
                    {
                        return false; //abort closing
                    }
                    else /// [just hide. OR hide and close] with or without saving
                    {
                        ///before closing save R scripts in Syntax Editor text area..13Feb2013
                        if (dresult == System.Windows.Forms.DialogResult.Yes)//Save
                            SyntaxEditorSaveAs();
                    }
                   
                }
                return true;
        }

        #region Find Replace
        FindReplaceWindow frw = null;
        private void findreplace_Click(object sender, RoutedEventArgs e)
        {
            if (frw == null)
            {
                frw = new FindReplaceWindow(this);
                frw.Owner = this;
                frw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            }
            frw.Show();
        }

        int currentidx = 0;
        StringBuilder sbfultxt = null; // initialized in constructor
        string findtext = string.Empty;
        public bool FindText(string texttofind, bool calledFromReplace=false)
        {
            bool isFound=true;
            findtext = texttofind;
            string fulltext = inputTextbox.Text;
            //sbfultxt = new StringBuilder(mytextbox.Text);
            sbfultxt.Clear();
            sbfultxt.Append(fulltext);

            int foundidx = fulltext.IndexOf(findtext, currentidx);
            int lastidx = fulltext.LastIndexOf(findtext);
            if (currentidx == 0 && foundidx < 0)
            {
                if (!calledFromReplace)
                {
                    MessageBox.Show("'" + findtext + "' not found!", "Not Found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                return false;
            }

            if (foundidx >= 0) // if text is found
            {
                inputTextbox.ScrollToLine(inputTextbox.GetLineIndexFromCharacterIndex(foundidx));//scroll to the line where text is
                inputTextbox.Select(foundidx, findtext.Length);
                currentidx = foundidx + findtext.Length;
            }

            //if end is reached reset the current index
            if (currentidx >= fulltext.Length || (currentidx > 0 && foundidx < 0))
            {
                currentidx = 0;
                isFound = false;
            }
            inputTextbox.Focus();
            return isFound;
        }

        public bool ReplaceWith(string texttofind, string replacewith)
        {
            string replacetext = replacewith;
            findtext = texttofind; 
            if (sbfultxt.Length > 0 && replacetext.Length > 0 && findtext.Length > 0)
            {
                int strtidx = (currentidx - findtext.Length - 1);
                if (strtidx < 0 || strtidx > sbfultxt.Length)
                {
                    strtidx = 0;
                }
               sbfultxt.Replace(findtext, replacetext, strtidx, (findtext.Length + 1));
               inputTextbox.Text = sbfultxt.ToString();
            }

            return FindText(texttofind, true);//return 'false' if ther are no more entries found, to replace.
        }

        public void CloseFindReplace()
        {
            frw=null;
        }
        #endregion

        #region refresh recent file list 21feb2013

        private void initRecentSyntaxFileHandler()
        {
            //Recent files setting for Syntax Editor script files.
            recentSyntaxfiles = new RecentDocs();//21Feb2013
            recentSyntaxfiles.MaxRecentItems = 7;
            recentSyntaxfiles.XMLFilename = string.Format(@"{0}SyntaxRecent.xml", BSkyAppData.BSkyDataDirConfigFwdSlash);//23Apr2015 @"./Config/Recent.xml";
            recentSyntaxfiles.recentitemclick = SyntaxRecentItem_Click;
            RefreshRecent();//
        }

        public void RefreshRecent()
        {
            MenuItem recent = GetMenuItemByHeaderPath("_File>Recent");
            try
            {
                recentSyntaxfiles.RecentMI = recent;
            }
            catch (Exception ex)//17Jan2014
            {
                MessageBox.Show("SyntaxRecent.xml not found...");
                logService.WriteToLogLevel("SyntaxRecent.xml not found.\n" + ex.StackTrace, LogLevelEnum.Fatal);
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
                foreach (MenuItem itm in SMenu.Items)
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

        private void SyntaxRecentItem_Click(string fullpathfilename)
        {
            if (System.IO.File.Exists(fullpathfilename))
            {
                System.IO.StreamReader file = new System.IO.StreamReader(fullpathfilename);
                inputTextbox.Text = file.ReadToEnd();
                file.Close();
                Modified = false;//19Feb2013 Newly loaded script can only be modified after loading finishes.
                SyntaxTitle.Text = syntitle + fullpathfilename; //19Feb2013
            }
            else
            {
                MessageBox.Show(fullpathfilename + " does not exists!", "File not found!", MessageBoxButton.OK, MessageBoxImage.Warning);
                //If file does not exists. It should be removed from the recent files list.
                recentSyntaxfiles.RemoveXMLItem(fullpathfilename);
            }
        }

        #endregion

        #endregion

        #region Split orientation handler
        bool isHorizontal = false;

        private void flip_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi.Name.Trim().Equals("vertical"))
            {
                toVerticalSplit();
                horizontalsplit.Visibility = System.Windows.Visibility.Collapsed;
                verticalsplit.Visibility = System.Windows.Visibility.Visible;
                isHorizontal = false;
                vertical.IsEnabled = false;
                horizontal.IsEnabled = true;
            }
            else if (mi.Name.Trim().Equals("horizontal"))
            {
                toHorizontalSplit();
                horizontalsplit.Visibility = System.Windows.Visibility.Visible;
                verticalsplit.Visibility = System.Windows.Visibility.Collapsed;
                isHorizontal = true;
                vertical.IsEnabled = true;
                horizontal.IsEnabled = false;
            }
            else
            {
                return;
            }

            e.Handled = true;
        }

        private void toVerticalSplit()
        {
            splitimg.Source = new BitmapImage(new Uri("pack://application:,,,/BlueSky;component/Images/splitV.png"));
            double winwidth = this.Width;
            double outpanelWidth = (.7 * winwidth); //10% of the total width of the window is assicgned to syantax in vertical orientation. 90% for stackpanel for output
            rightmost.Width = new GridLength(.55, GridUnitType.Star);//star
            leftmost.Width = new GridLength(1, GridUnitType.Star);//new GridLength(outpanelWidth, GridUnitType.Pixel);
            top.Height = new GridLength(1, GridUnitType.Star);
            bottom.Height = new GridLength(0, GridUnitType.Pixel);
            rightmost.MinWidth = 25;


            Grid.SetRow(syntaxgrid, 0);
            Grid.SetColumn(syntaxgrid, 1);
            //try adding col and row span
            syntaxgrid.SetValue(Grid.ColumnSpanProperty, 1);
            syntaxgrid.SetValue(Grid.RowSpanProperty, 2);
            outputgrid.SetValue(Grid.ColumnSpanProperty, 1);
            outputgrid.SetValue(Grid.RowSpanProperty, 2);

        }

        private void toHorizontalSplit()
        {
            splitimg.Source = new BitmapImage(new Uri("pack://application:,,,/BlueSky;component/Images/splitH.png"));
            rightmost.Width = new GridLength(0, GridUnitType.Pixel);
            leftmost.Width = new GridLength(1, GridUnitType.Star);
            top.Height = new GridLength(1, GridUnitType.Star);
            bottom.Height = new GridLength(.4, GridUnitType.Star);//star
            bottom.MinHeight = 25;


            Grid.SetRow(syntaxgrid, 1);
            Grid.SetColumn(syntaxgrid, 0);
            //try adding col and row span
            syntaxgrid.SetValue(Grid.ColumnSpanProperty, 2);
            syntaxgrid.SetValue(Grid.RowSpanProperty, 1);
            outputgrid.SetValue(Grid.ColumnSpanProperty, 2);
            outputgrid.SetValue(Grid.RowSpanProperty, 1);

        }
        #endregion
        
        #region Collapse Syntax

        double oldbottomheight;
        double oldrightmost;
        double oldleftmost;
        bool isSyntaxCollapsed = false;

        private void CollapseSyntax()
        {
            //is horizontal rowdefsyntax
            if (isHorizontal)
            {
                if (syntaxgrid.ActualHeight < 40) isSyntaxCollapsed = true; else isSyntaxCollapsed = false;
                if (!isSyntaxCollapsed)
                {
                    oldbottomheight = bottom.ActualHeight;//store old height

                    bottom.Height = new GridLength(25, GridUnitType.Pixel);//star
                    bottom.MinHeight = 25;
                    isSyntaxCollapsed = true;
                }
                else
                {
                    top.Height = new GridLength(1, GridUnitType.Star);//star
                    bottom.Height = new GridLength(oldbottomheight, GridUnitType.Pixel);//star
                    isSyntaxCollapsed = false;
                }
            }
            else //is vertical
            {
                if (syntaxgrid.ActualWidth < 40) isSyntaxCollapsed = true; else isSyntaxCollapsed = false;

                if (!isSyntaxCollapsed)
                {
                    rightmost.MinWidth = 25;
                    oldrightmost = rightmost.ActualWidth;
                    oldleftmost = leftmost.ActualWidth;
                    rightmost.Width = new GridLength(25, GridUnitType.Pixel);
                    leftmost.Width = new GridLength(1, GridUnitType.Star);
                    isSyntaxCollapsed = true;
                }
                else
                {
                    rightmost.Width = new GridLength(oldrightmost, GridUnitType.Pixel);
                    leftmost.Width = new GridLength(1, GridUnitType.Star);
                    isSyntaxCollapsed = false;
                }
            }
        }


        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CollapseSyntax();
        }


        #endregion

        #region Navigation tree Show/hide
        private bool navtreehidden = true;
        private void navtreemi_Click(object sender, RoutedEventArgs e)
        {
            if (navtreehidden)
            {
                navtreecol.Width = new GridLength(150, GridUnitType.Pixel);
                navtreecol.MinWidth = 10;
                navtreemi.Header = "Hide Navigation Tree";
                navtreehidden = false;
            }
            else
            {
                navtreecol.MinWidth = 0;
                navtreecol.Width = new GridLength(0, GridUnitType.Pixel);
                
                navtreemi.Header = "Show Navigation Tree";
                navtreehidden = true;
            }
        }
        #endregion

        #region Window Loaded  & Maximised/Restore
        //window minimised, maximised and restored. So when maximised syntax appears right but when 'restore' syntax become invible
        private void outwin_SizeChanged(object sender, SizeChangedEventArgs e)
        {


        }

        private void outwin_Loaded(object sender, RoutedEventArgs e)
        {
            oldrightmost = this.Width * .4;
            oldbottomheight = this.Height * .4;
        }

        #endregion





    }
    public class PropertyDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultnDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item,
                   DependencyObject container)
        {
            AnalyticsData dpi = item as AnalyticsData;

            return DefaultnDataTemplate;
        }
    }

    public class Employee
    {
        public string name { get; set; }
        public int age { get; set; }
        public string city { get; set; }
    }
}
