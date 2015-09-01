using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BSky.Statistics.Service.Engine.Interfaces;
using BSky.Statistics.Common;
using System.Xml;
using BSky.Interfaces.Commands;
using BSky.Controls;
using BlueSky.Services;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.IO;
using BlueSky.Commands.Output;
using BSky.Controls.Controls;
using System.Collections.Generic;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using BSky.OutputGenerator;
using BlueSky.Commands.File;
using System.Windows.Media;
using System.Globalization;
using BlueSky.Commands;
using BSky.Interfaces.Interfaces;
using BSky.DynamicClassCreator;
using System.Collections;
using C1.WPF.FlexGrid;
using System.Text;
using RDotNet;
using BSky.Lifetime.Services;
using System.Windows.Input;
using BSky.Interfaces.Services;


namespace BlueSky
{
    public enum RCommandType { RCOMMAND,  CONDITIONORLOOP, BSKYFORMAT, BSKYLOADREFRESHDATAFRAME, BSKYREMOVEREFRESHDATAFRAME, GRAPHIC, GRAPHICXML, SPLIT, REFRESHGRID, RDOTNET }
    /// <summary>
    /// Interaction logic for SyntaxEditorWindow.xaml
    /// </summary>
    public partial class SyntaxEditorWindow : Window
    {
        IAnalyticsService analytics = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
        IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//23nov2012
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//13Dec2012
        // for checking license validity. NOTE: License check must be done before to set bool flag
        // that we are doing by invoking license check at app launch time

        bool AdvancedLogging;

        CommandRequest sinkcmd = new CommandRequest();
        C1.WPF.FlexGrid.FileFormat fileformat = C1.WPF.FlexGrid.FileFormat.Html;
        bool extratags = true; // true means default file type will be .BSO
        string fullfilepathname = string.Empty;
        //List<string> registeredGraphicsList = new List<string>();//for all graphics listed in GraphicCommandList.txt 07Sep2012
        Dictionary<string, ImageDimensions> registeredGraphicsList = new Dictionary<string, ImageDimensions>();//28May2015

        OutputMenuHandler omh = new OutputMenuHandler();//Output menu
        bool SEForceClose = false;//05Feb2013
        bool Modified = false; //19Feb2013 to track if any modification has been done after last save
        bool bsky_no_row_header; //14Jul2014 for supressing the default rowheaders "1","2","3" whenever "bsky_no_row_header" is passed as a parameter in BSkyFormat().
        long EMPTYIMAGESIZE = 318;// bytes
        int _currentGraphicWidth = 600;//current width of the image
        int _currentGraphicHeight = 600;//current height of the image

        string doubleClickedFilename;//17May2013
        public string DoubleClickedFilename
        {
            get;
            set;
        }

        BSkyDialogProperties DlgProp; // for storing dialog properties, needed to decide when to refresh grid/status or print output etc..
        FrameworkElement felement;
        public FrameworkElement FElement //for some commands its is important to set this property
        {
            get { return felement; }
            set { felement = value; }
        }

        object menuParameter;
        public object MenuParameter
        {
            get { return menuParameter; }

            set { menuParameter = value; }
        }
        //15Nov2013 for storing all commands those got executed when RUN button was cliced once
        SessionOutput sessionlst;
        OutputWindow ow;

        //07Nov2014 To Get SessionList items count at any point.
        public int SesssionListItemCount 
        {
            get { return sessionlst.Count; } 
        }

        public SyntaxEditorWindow()
        {
            InitializeComponent();
            this.MinWidth = 440;// 384;
            this.MinHeight = 200;
            this.Width = 750;// 976;
            this.Height = 550;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            SMenu.Items.Add(omh.OutputMenu);///Add Output menu ///
            inputTextbox.Focus();//set focus inside text box.

            //opening Graphics device with sytax Editor. //05May2013
            // when graphic command executes, we close the device then generate output and finally open the device again.
            OpenGraphicsDevice();
            sessionlst = new SessionOutput();

        }

        public bool SynEdtForceClose
        {
            get { return SEForceClose; }
            set { SEForceClose = value; }
        }

        private void runButton_Click(object sender, RoutedEventArgs e)
        {
            /////Selected or all commands from textbox////30Apr2013
            string commands = inputTextbox.SelectedText;//selected text
            if (commands != null && commands.Length > 0)
            {

            }
            else
            {
                commands = inputTextbox.Text;//All text
            }
            if (commands.Trim().Length > 0)
            {
                RunCommands(commands);
                DisplayAllSessionOutput();//22Nov2013
            }
        }

        public void RunCommands(string commands, BSkyDialogProperties dlgprop = null) //30Apr2013
        {
            try
            {
                ShowMouseBusy();

                AdvancedLogging = AdvancedLoggingService.AdvLog;//01May2015
                logService.WriteToLogLevel("Adv Log Flag:" + AdvancedLogging.ToString(), LogLevelEnum.Info);

                DlgProp = dlgprop;

                #region Load registered graphic commands from GraphicCommandList.txt 18Sep2012
                // loads each time run is clicked. Performance will be effected, as we read file each time.
                string grplstfullfilepath = confService.GetConfigValueForKey("sinkregstrdgrph");//23nov2012
                //if graphic file does not exists the n create one.
                if (!IsValidFullPathFilename(grplstfullfilepath, true))//17Jan2014
                {
                    string text = "plot";
                    System.IO.File.WriteAllText(@grplstfullfilepath, text);
                }

                // load default value if no path is set or invalid path is set
                if (grplstfullfilepath.Trim().Length == 0 || !IsValidFullPathFilename(grplstfullfilepath, true))
                {
                    //grplstfullfilepath = confService.DefaultSettings["sinkregstrdgrph"];
                    MessageBox.Show(this, "Key 'sinkregstrdgrph' not found in config file. You cannot run Graphics from Command Editor.");
                    //return;
                }
                else
                {
                    LoadRegisteredGraphicsCommands(@grplstfullfilepath);
                }
                #endregion

                #region Save to Disk
                if (saveoutput.IsChecked == true)
                {
                    if (fullpathfilename.Text != null && fullpathfilename.Text.Trim().Length > 0)
                    {
                        fullfilepathname = fullpathfilename.Text;///setting filename
                        bool fileExists = File.Exists(fullfilepathname); fileExists = false;
                        if (fullfilepathname.Contains('.') && !fileExists)
                        {
                            string extension = Path.GetExtension(fullfilepathname).ToLower();// fullfilepathname.Substring(fullfilepathname.LastIndexOf('.'));
                            if (extension.Equals(".csv"))
                            { fileformat = C1.WPF.FlexGrid.FileFormat.Csv; extratags = false; }
                            else if (extension.Equals(".html"))
                            { fileformat = C1.WPF.FlexGrid.FileFormat.Html; extratags = false; }
                            else if (extension.Equals(".bsoz"))
                            { fileformat = C1.WPF.FlexGrid.FileFormat.Html; extratags = true; }
                            else
                            { fileformat = C1.WPF.FlexGrid.FileFormat.Html; extratags = true; fullfilepathname = fullfilepathname + ".bsoz"; }
                        }
                        else
                        {
                            MessageBox.Show(this, "Output File Already Exists! Provide different name in Command Editor window.");
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show(this, "Please provide new output filename and fileformat by clicking 'Browse' in Command Editor for saving the output.", "Save Output is checked...", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        return;
                    }
                }
                #endregion

                #region Get Active output Window
                //////// Active output window ///////
                OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
                ow = owc.ActiveOutputWindow as OutputWindow; //get currently active window
                if (saveoutput.IsChecked == true)
                {
                    ow.ToDiskFile = true;//save lst to disk. Dump
                }
                #endregion

                #region Executing Syntax Editors Commands
                ///// Now statements from Syntax Editor will be executed ////
                CommandOutput lst = new CommandOutput(); ////one analysis////////
                lst.IsFromSyntaxEditor = true;
                if (saveoutput.IsChecked == true)//10Jan2013
                    lst.SelectedForDump = true;

                ////03Oct2014 We should remove R comments right here, before proceeding with execution.
                string nocommentscommands = RemoveCommentsFromCommands(commands);

                ExecuteCommandsAndCreateSinkFile(ow, lst, nocommentscommands);
                bool s = true;
                if (s) CreateOuput(ow); /// for last remaining few non BSkyFormat commands, if any.
                /// 

                #endregion

                #region Saving to Disk
                //////Dumping results from Syntax Editor ////08Aug2012
                if (saveoutput.IsChecked == true)
                    ow.DumpAllAnalyisOuput(fullfilepathname, fileformat, extratags);
                #endregion
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("Exeception:" + ex.Message, LogLevelEnum.Error);
            }
            finally
            {
                HideMouseBusy();
            }
        }

        #region Mouse Busy - Mouse Free
        Cursor defaultcursor;
        private void ShowMouseBusy()
        {
            defaultcursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }
        //Hides Progressbar
        private void HideMouseBusy()
        {
            Mouse.OverrideCursor = null;
        }
        #endregion

        //03Oct2014 Removing comments from the selection ( selected commands in textbox control in syntax editor )
        private string RemoveCommentsFromCommands(string alltext)
        {
            StringBuilder final = new StringBuilder();
            char[] splitchars = { '\r', '\n', ';' };
            string[] lines = alltext.Split(splitchars);
            int len = lines.Length;
            for (int i = 0; i < len; i++)
            {
                if (lines[i] != null && lines[i].Length > 0)
                    final.AppendLine(RemoveComments(lines[i].Trim()));// +"\r\n";
            }

            return(final.ToString()); 
        }

        //Checks if number of different types of brakets have openbracket count == closebracket count.
        //return bool = false when there is mis-match in count of any type of bracktes.
        //Each bracket count = openbracketcount - closebracketcount. So zero means no mismatch. 
        //+ve and -ve count means either one of the open or close bracket, has more/less counts.
        private bool AreBracketsBalanced(string commands, out string msg) //out int roundbrackets, out int curlybrackets, out int squarebrackets)
        {
            bool balanced = true;//assuming all brackets have open bracket count = close bracket count(for each type of bracket).
            int roundbrackets = 0;
            int curlybrackets = 0;
            int squarebrackets = 0;

            //loop thru char by char to find each type
            foreach (char ch in commands)
            {
                if (ch == '(') roundbrackets++;
                else if (ch == ')') roundbrackets--;
                else if (ch == '{') curlybrackets++;
                else if (ch == '}') curlybrackets--;
                else if (ch == '[') squarebrackets++;
                else if (ch == ']') squarebrackets--;
            }

            if (roundbrackets != 0 || curlybrackets != 0 || squarebrackets != 0)
                balanced = false;

            ////Generating error message based on counts
            string msg1=string.Empty, msg2=string.Empty, msg3=string.Empty;
            if (roundbrackets > 0)
            {
                msg1 = "missing ')'";
            }
            if (roundbrackets < 0)
            {
                msg1 = "unexpected ')' ";
            }
            if (curlybrackets > 0)
            {
                msg2 = "missing '}'";
            }
            if (curlybrackets < 0)
            {
                msg2 = "unexpected '}' ";
            }
            if (squarebrackets > 0)
            {
                msg3 = "missing ']'";
            }
            if (squarebrackets < 0)
            {
                msg3 = "unexpected ']' ";
            }


            msg = msg1 + " " + msg2 + " " + msg3;

            return balanced;
        }

        //22Nov2013 for sending session contents to output window.
        //This gives us control to send output each time "RUN" is clicked and for batch command 
        //this method can be called when last slice contents get appended to sessionlist
        public void DisplayAllSessionOutput(string sessionheader = "", OutputWindow selectedOW=null)//06May2015 slectedOW param added
        {
            sessionlst.NameOfSession = sessionheader;
            sessionlst.isRSessionOutput = true;
            //if (sessionheader.Trim().Length > 1)
            if(sessionlst.Count>0)//07Nov2014
            {
                if (selectedOW == null)
                {
                    //for dialog batch commads say 'lm' sessionheader will have some dialog title like 'lm'
                    ow.AddSynEdtSessionOutput(sessionlst);//send output to 'Active' marked output window.
                }
                else
                {
                    selectedOW.AddSynEdtSessionOutput(sessionlst);//send output to specific window from where command was executed
                }
            }
            sessionlst = new SessionOutput();//28Nov2013 for creating  new instance and not deleting old one
        }

        //////////pull all the currently loaded datasets names //////////
        private string getActiveDatasetNames()
        {
            string allDatasetnames = string.Empty;
            UIControllerService layoutController = LifetimeService.Instance.Container.Resolve<IUIController>() as UIControllerService;
            foreach (TabItem ti in (layoutController.DocGroup.Items))
            {
                if (allDatasetnames.Trim().Length < 1)
                    allDatasetnames = "[" + (ti.Tag as DataSource).Name + "] - " + (ti.Tag as DataSource).FileName;//+ " (" + (ti.Tag as DataSource).Name + ")";//First dataset
                else
                    allDatasetnames = allDatasetnames + "\n" + "[" + (ti.Tag as DataSource).Name + "] - " + (ti.Tag as DataSource).FileName;// +" (" + (ti.Tag as DataSource).Name + ")";//Second onwards
            }
            return allDatasetnames;
        }

        /// Delete old existing imagexxx.png files just before launching graphic device (each time) ///06May2013
        private void DeleteOldGraphicFiles()
        {
            string synedtimg = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("sinkimage"));//23nov2012 
            string tempDir = System.IO.Path.GetTempPath().Replace("\\", "/"); //confService.GetConfigValueForKey("tempfolder");

            /////Replace %03d , %04d with * wildcard ///
            int percentindex = synedtimg.IndexOf("%");
            int dindex = synedtimg.IndexOf("d", percentindex);
            string percentstr = synedtimg.Substring(percentindex, (dindex - percentindex + 1));
            string tempsynedtimg = Path.GetFileName(synedtimg).Replace(percentstr, "*");
            //Delete all image Files in temp Folder ///
            foreach (FileInfo fi in new DirectoryInfo(tempDir).GetFiles(tempsynedtimg))
            {
                DeleteFileIfPossible(@fi.FullName);
            }
        }

        int  GraphicDeviceImageCounter = 0;//to keep track of the next image file name.
        //Openes new PNG graphics device. This function can be customised to open any graphics device.
        private void OpenGraphicsDevice(int imagewidth=0, int imageheight=0)//05May2013
        {
            DeleteOldGraphicFiles();//06May2013
            /////// These lines moved here from isGrap block to entertain multi graph in one image ////// 02May2013
            CommandRequest grpcmd = new CommandRequest();
            string synedtimg = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("sinkimage"));//23nov2012
            // load default value if no path is set or invalid path is set
            if (synedtimg.Trim().Length == 0 || !IsValidFullPathFilename(synedtimg, false))
            {
                MessageBox.Show(this, "Key 'sinkimage' not found in config file. Aborting...");
                return;
            }

            //27May2015. if parameters are passed, parameter values will take over ( overrides the values set through 'Options'
            if (imageheight > 0 && imagewidth > 0)
            {
                _currentGraphicWidth = imagewidth;
                _currentGraphicHeight = imageheight;
            }
            else // use dimenstions set in 'Options' config. IF thats absent then use 580 as default.
            {
                _currentGraphicWidth = 580;
                _currentGraphicHeight = 580;//defaults
                //get image size from config
                string imgwidth = confService.GetConfigValueForKey("imagewidth");//
                string imgheight = confService.GetConfigValueForKey("imageheight");//

                // load default value if no value is set or invalid value is set
                if (imgwidth.Trim().Length != 0)
                {
                    Int32.TryParse(imgwidth, out _currentGraphicWidth);
                }
                if (imgheight.Trim().Length != 0)
                {
                    Int32.TryParse(imgheight, out _currentGraphicHeight);
                }

            }

            grpcmd.CommandSyntax = "png(\"" + synedtimg + "\", width=" + _currentGraphicWidth + ",height=" + _currentGraphicHeight + ")";
            analytics.ExecuteR(grpcmd, false, false);

            //close graphic device to get the size of empty image and reopen it to keep it open for rest of the graphic processing.
            CloseGraphicsDevice();
            
            // Basically, make sure to find the exact first image name(with full path) that is created when graphic device is opened.
            string tempimgname = synedtimg.Replace("%03d", "001");//this is hard code but should be good till you keep %03d. Otherwise write logic for substitution
            if (File.Exists(tempimgname))
            {
                EMPTYIMAGESIZE = new FileInfo(tempimgname).Length;
            }
            EMPTYIMAGESIZE = EMPTYIMAGESIZE + 10;//increase it by some bytes just to make sure comparision does not fail.

            //Now finally open graphic device to actually wait for graphic command to execute and capture it.
            grpcmd.CommandSyntax = "png(\"" + synedtimg + "\", width=" + _currentGraphicWidth + ",height=" + _currentGraphicHeight + ")";
            analytics.ExecuteR(grpcmd, false, false);
            GraphicDeviceImageCounter = 0;//09Jun2015
        }

        //Closes current graphics device
        private void CloseGraphicsDevice()
        {
                CommandRequest grpcmd = new CommandRequest();
                //09Jun2015 here if(dev.cur()[[1]] == 2) dev.off() means that if device 2 is active then close it. 
                //that is, only if device number 2 (which is PNG in our case) is set then only try closing the current graphic device.
                // graphic device 1 is always default graphic device which cannot be closed. You get error if you do so.
                grpcmd.CommandSyntax = "if(dev.cur()[[1]] == 2) dev.off()";//09Jun2015 "dev.off()"; // "graphic.off()"; //
                analytics.ExecuteR(grpcmd, false, false);
        }

        //check file size of PNG file generated by R
        private long GetGraphicSize()//05May2013
        {
            long size=0;
            CommandRequest grpcmd = new CommandRequest();
            string synedtimg = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("sinkimage"));//23nov2012
            // load default value if no path is set or invalid path is set
            if (synedtimg.Trim().Length == 0 || !IsValidFullPathFilename(synedtimg, false))
            {
                MessageBox.Show(this,"Key 'sinkimage' not found in config file. Aborting...");
                return 0;
            }
            if(File.Exists(synedtimg))
            {
                size = new FileInfo(synedtimg).Length;
            }
            return size;
        }

        //if in config window the image size has been altered,
        //we must close graphic device and open it again with new dimentions from config
        public void RefreshImgSizeForGraphicDevice()
        {
            int newwidth=10;
            int newheight=10;
            //get image size from config
            string imgwidth = confService.GetConfigValueForKey("imagewidth");//
            string imgheight = confService.GetConfigValueForKey("imageheight");//
            // load default value if no value is set or invalid value is set
            if (imgwidth.Trim().Length != 0)
            {
                Int32.TryParse(imgwidth, out newwidth);
            }
            if (imgheight.Trim().Length != 0)
            {
                Int32.TryParse(imgheight, out newheight);
            }

            if (_currentGraphicWidth != newwidth || _currentGraphicHeight != newheight) // if config setting modified
            {
                //close graphic device and open it again, it will automatically take new size and set them as _current...  .
                CloseGraphicsDevice();
                OpenGraphicsDevice();
            }
        }

        //18Nov2013 Add BSky OSMT and CrossTab to Session. This output is return back from OutputWindow
        public void AddToSession(CommandOutput co)
        {
            if (co != null && co.Count > 0)
            {
                sessionlst.Add(co);
                co = new CommandOutput();//after adding to session new object is allocated for futher output creation
            }
        }

        private void ExecuteCommandsAndCreateSinkFile(OutputWindow ow, CommandOutput lst, string seltext)//sending message and output to sink file
        {
            string objectname;
            seltext = seltext.Replace('\n', ';').Replace('\r', ' ').Trim();
            seltext = JoinCommaSeparatedStatment(seltext);
            string stmt = "";
            //////wrap in sink////////
            string sinkfilefullpathname = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("tempsink"));//23nov2012
            // load default value if no path is set or invalid path is set
            if (sinkfilefullpathname.Trim().Length == 0 || !IsValidFullPathFilename(sinkfilefullpathname, false))
            {
                //sinkfilefullpathname = confService.DefaultSettings["tempsink"];
                MessageBox.Show(this, "Key 'tempsink' not found in config file. Aborting...");
                return;
            }
            OpenSinkFile(@sinkfilefullpathname, "wt");
            SetSink();
            string _command = string.Empty;//05May2013
            int bskyfrmtobjcount = 0;
            bool breakfor = false;//, continuefor=false;//14Nov2013
            for (int start = 0, end = 0; start < seltext.Length; start = start + end + 1) //28Jan2013 final condition was start < seltext.Length-1
            {
                objectname = "";
                end = seltext.IndexOf(';', start) - start;
                if (end < 0) // if ; not found
                    end = seltext.IndexOf('\n', start) - start;
                if (end < 0)// if new line not found
                    end = seltext.Length - start;
                stmt = seltext.Substring(start, end).Replace('\n', ' ').Replace('\r', ' ').Trim();

                if (stmt.Trim().Length < 1 || stmt.Trim().IndexOf('#') == 0)
                    continue;

                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Syntax going to execute : " + stmt, LogLevelEnum.Info);

                if (stmt.Trim().IndexOf('#') > 1) //12May2014 if any statment has R comments in the end in same line.
                    stmt = stmt.Substring(0, stmt.IndexOf("#"));

                object o = null;


                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Categorizing command before execution.", LogLevelEnum.Info);

                _command = ExtractCommandName(stmt);//07sep2012
                RCommandType rct = GetRCommandType(_command);

                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Syntax command category : " + rct.ToString(), LogLevelEnum.Info);

                try
                {
                    switch (rct)
                    {
                        case RCommandType.CONDITIONORLOOP:  //Block Commands
                            int end2 = end;
                            stmt = CurlyBracketParser(seltext, start, ref end);
                            if (stmt.Equals("ERROR"))
                            {
                                breakfor = true;
                            }
                            else
                            {
                                SendCommandToOutput(stmt, "R-Command");
                                ExecuteOtherCommand(ow, stmt);
                            }

                            //02Dec2014
                            ResetSink();
                            CloseSinkFile();
                            CreateOuput(ow);
                            OpenSinkFile(@sinkfilefullpathname, "wt");
                            SetSink();
                            ///02Dec2015 Now add graphic (all of them, from temp location)
                            CreateAllGraphicOutput(ow);//get all grphics and send to output
                            break;
                        case RCommandType.GRAPHIC:
                            CommandRequest grpcmd = new CommandRequest();

                            grpcmd.CommandSyntax = "write(\"" + stmt.Replace("<", "&lt;").Replace('"', '\'') + "\",fp)";// http://www.w3schools.com/xml/xml_syntax.asp
                            o = analytics.ExecuteR(grpcmd, false, false); //for printing command in file
                            CloseGraphicsDevice();
                            //GetGraphicSize()
                            ResetSink();
                            CloseSinkFile();
                            CreateOuput(ow);
                            OpenSinkFile(@sinkfilefullpathname, "wt");
                            SetSink();
                            OpenGraphicsDevice();//05May2013
                            //continuefor = true;
                            //continue;
                            break;
                        case RCommandType.GRAPHICXML:
                            ExecuteXMLTemplateDefinedCommands(stmt);
                            break;
                        case RCommandType.BSKYFORMAT:
                            ResetSink();
                            CloseSinkFile();
                            CreateOuput(ow);
                            SendCommandToOutput(stmt, "BSkyFormat");//26Aug2014 blue colored
                            ExecuteBSkyFormatCommand(stmt, ref bskyfrmtobjcount, ow); // this should be out of block and so "TRUE" must be passed in BSkyFormat
                            OpenSinkFile(@sinkfilefullpathname, "wt");
                            SetSink();
                            break;
                        case RCommandType.BSKYLOADREFRESHDATAFRAME: //BSkyLoadRefreshDataframe(dfname)
                            ResetSink();
                            CloseSinkFile();
                            CreateOuput(ow);
                            SendCommandToOutput(stmt, "Load-Refresh Dataframe");//26Aug2014 blue colored
                            bool success = ExecuteBSkyLoadRefreshDataframe(stmt);
                            if (!success)
                                SendErrorToOutput("Error:Cannot Load/Refresh Dataset. Dataframe does not exists. OR not 'data.frame' type.", ow); //03Jul2013
                            OpenSinkFile(@sinkfilefullpathname, "wt");
                            SetSink();
                            break;
                        case RCommandType.BSKYREMOVEREFRESHDATAFRAME: //BSkyRemoveRefreshDataframe(dfname)
                            ResetSink();
                            CloseSinkFile();
                            CreateOuput(ow);
                            ExecuteBSkyRemoveRefreshDataframe(stmt);
                            OpenSinkFile(@sinkfilefullpathname, "wt");
                            SetSink();
                            break;
                        case RCommandType.SPLIT: // set/remove split and refresh status bar in main window showing split vars
                            ResetSink();
                            CloseSinkFile();
                            CreateOuput(ow);
                            ExecuteSplit(stmt);
                            OpenSinkFile(@sinkfilefullpathname, "wt");
                            SetSink();
                            break;
                        case RCommandType.RCOMMAND:
                            SendCommandToOutput(stmt, "R-Command");

                            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Categorized. Before execution.", LogLevelEnum.Info);

                            ExecuteOtherCommand(ow, stmt);

                            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Categorized. After execution.", LogLevelEnum.Info);

                            ResetSink();
                            CloseSinkFile();
                            CreateOuput(ow);
                            OpenSinkFile(@sinkfilefullpathname, "wt");
                            SetSink();

                            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Categorized. Before getting graphic if any.", LogLevelEnum.Info);
                            ///02Dec2015 Now add graphic (all of them, from temp location)
                            CreateAllGraphicOutput(ow);//get all grphics and send to output
                            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Categorized. After getting graphic if any.", LogLevelEnum.Info);

                            break;
                        
                        case RCommandType.RDOTNET:
                            InitializeRDotNet();
                            RDotNetOpenDataset();
                            RDotNetExecute(ow);
                            DisposeRDotNet();
                            break;
                        default:
                            break;
                    }//switch
                }
                catch (Exception exc)
                {
                    SendCommandToOutput(exc.Message, "Error:");
                    logService.WriteToLogLevel("Error executing: " + _command, LogLevelEnum.Error);
                    logService.WriteToLogLevel(exc.Message, LogLevelEnum.Error);
                }

                if (breakfor)
                    break;
            }/////for

           //////wrap in sink////////
            ResetSink();
            CloseSinkFile();
        }

        //13Nov2013
        private RCommandType GetRCommandType(string _command)
        {
            RCommandType rct;
            if (isConditionalOrLoopingCommand(_command))
                rct = RCommandType.CONDITIONORLOOP;
            else if (isGraphicCommand(_command))
            {
                if (isXMLDefined())
                {
                    rct = RCommandType.GRAPHICXML;//graphic like bsky.plot & bsky.fullhistogram
                }
                else
                {
                    rct = RCommandType.RCOMMAND; //
                }
            }
            else if (_command.Contains("BSkyFormat("))
                rct = RCommandType.BSKYFORMAT;
            else if (_command.Contains("BSkyLoadRefreshDataframe("))
                rct = RCommandType.BSKYLOADREFRESHDATAFRAME;
            else if (_command.Contains("BSkyRemoveRefreshDataframe("))
                rct = RCommandType.BSKYREMOVEREFRESHDATAFRAME;
            else if (_command.Contains("BSkySetDataFrameSplit(")) //set or remove split
                rct = RCommandType.SPLIT;
            else if (_command.Contains("RDotNetTest"))
                rct = RCommandType.RDOTNET;
            //else if (_command.EndsWith(","))
            //    rct = RCommandType.RCOMMANDENDSINCOMMA;
            else
                rct = RCommandType.RCOMMAND;
            
            return rct;
        }

        /// Join the statments Ends in comma
        private string JoinCommaSeparatedStatment(string comm)//, int start, ref int end)
        {
            comm = Regex.Replace(comm, @",\s*;", ",");
            return comm;
        }

        ////curly block parser////
        private string CurlyBracketParser(string comm, int start, ref int end)
        {
            string str = string.Empty;
            int curlyopen = 0, curlyclose = 0;
            for (int i = comm.IndexOf('{', start); i < comm.Length; i++)
            {
                if (comm.ElementAt(i).Equals('{')) curlyopen++;
                else if (comm.ElementAt(i).Equals('}')) curlyclose++;
                if (curlyopen == curlyclose)
                {
                    end = i + 1 - start;
                    //if(start < comm.Length)
                    str = comm.Substring(start, end).Replace("}}", "} }").Replace(";{", "{").Replace("{;", "{").Replace("}", ";} ");
                    start = i + 1;
                    break;
                }
            }
            if (curlyopen != curlyclose)
            {
                CommandRequest cmdprn = new CommandRequest();
                cmdprn.CommandSyntax = "write(\"Error in block declaration. Mismatch { or }\",fp)";
                analytics.ExecuteR(cmdprn, false, false); /// for printing command in file
                return "ERROR";
            }
            str = Regex.Replace(str, @";+", ";");//multi semicolon to one ( no space between them)
            //str = Regex.Replace(str, @"}\s+;", "} ");//semicolon after close }
            str = Regex.Replace(str, @";\s*;", ";");//multi semicolon to one(space between them)
            str = Regex.Replace(str, @"}\s*;\s*}", "} }");//semicolon between two closing } }
            str = Regex.Replace(str, @"{\s*;", "{ ");//semicolon immediatly after opening {
            str = Regex.Replace(str, @";\s*{", "{ ");//semicolon immediatly after opening {
            if (str.Contains("else"))
            {
                str = Regex.Replace(str, @"}\s*;*\s*else", "} else");//semicolon before for is needed. Fix for weird bug.
                //str = Regex.Replace(str, @"\s*;*\s*else", "else");//semicolon before for is needed. Fix for weird bug.
            }
            ///if .. else if logic ///
            if ((str.Trim().StartsWith("if") || str.Trim().StartsWith("else")) && comm.Length > end + 1)
            {
                string elsestr = string.Empty;
                if (start + 1 < comm.Length)
                    elsestr = comm.Substring(start + 1);
                int originalLen = elsestr.Length;
                elsestr = Regex.Replace(elsestr, @";*\s*else", " else").Trim();
                int newLen = elsestr.Length;
                if (elsestr.StartsWith("else"))
                {
                    int end2 = 0;
                    str = str + CurlyBracketParser(elsestr, 0, ref end2);
                    end = end + end2 + (originalLen - newLen + 1);
                }
            }
            return str;
        }

        ////round bracket block parser////
        private string RoundBracketParser(string comm, int start, ref int end)
        {
            string str = string.Empty;//comm.Replace("))", ") )").Replace(";(", "(").Replace("(;", "(");
            int roundopen = 0, roundclose = 0;
            //if(start < comm.Length)
            for (int i = comm.IndexOf('(', start); i < comm.Length; i++)
            {
                if (i < 0)
                    continue;
                if (comm.ElementAt(i).Equals('(')) roundopen++;
                else if (comm.ElementAt(i).Equals(')')) roundclose++;

                if (roundopen == roundclose)//find ';' afer closing round bracket
                {
                    int idx = comm.IndexOf(";", i);
                    if (idx > i)
                    {
                        end = idx - start;
                        str = comm.Substring(start, end).Replace("))", ") )").Replace(";(", "(").Replace("(;", "(");
                    }
                    else
                    {
                        end = comm.Length - start;
                        str = comm.Substring(start, end).Replace("))", ") )").Replace(";(", "(").Replace("(;", "(");
                    }
                    break;
                }
            }
            if (roundopen != roundclose)
            {
                CommandRequest cmdprn = new CommandRequest();
                cmdprn.CommandSyntax = "write(\"Error in block declaration. Mismatch ( or )\",fp)";
                analytics.ExecuteR(cmdprn, false, false); /// for printing command in file
                return "ERROR";
            }
            str = str.Replace(";", " ");
            str = RemoveComments(str);
            return str;
        }

        private string RemoveComments_others(string str)//14May2014
        {
            if (str == null || str.Length < 1)
                return null;

            int len = str.Length;

            int sidx = str.IndexOf("#"); // very first, index of "#"
            int eidx = 0, remvlen = 0;
            if (sidx < 0) // if there is no comment
                return str;

            for (; ; )
            {
                eidx = str.IndexOf(";", sidx);
                remvlen = eidx - sidx;
                str = str.Remove(sidx, remvlen);

                sidx = str.IndexOf("#");
                if (sidx < 0)
                    break;
            }
            return str;
        }

        ////03Oct2014 New Remove comments logic
        private string RemoveComments(string text)
        {
            string nocommenttext = string.Empty;
            int openbracketcount = 0, singlequote = 0, doublequote = 0;
            if (text != null && text.Length > 0 && text.Contains('#'))
            {
                int idx = 0;
                for (idx = 0; idx < text.Length; idx++) // go character by character
                {
                    if (text[idx].Equals('(')) openbracketcount++;
                    else if (text[idx].Equals(')')) openbracketcount--;
                    else if (text[idx].Equals('\'')) singlequote++;
                    else if (text[idx].Equals('"')) doublequote++;
                    else if (text[idx].Equals('#'))
                    {
                        if (openbracketcount == 0 && singlequote % 2 == 0 && doublequote % 2 == 0) // # is outside any quotes or brackets
                        {
                            nocommenttext = text.Substring(0, idx);
                            break;
                        }
                    }

                }

            }
            else//that means there is no #-comment in that line.
            {
                nocommenttext = text;
            }
            return nocommenttext;
        }
        //// curly block logics ////NOT in Use
        private string BlockCodeParser(string seltext, int start, ref int end)
        {
            string stmt = string.Empty;
            string subs = seltext.Substring(start).Replace("}}", "} }").Replace(";{", "{").Replace("{;", "{");
            int blockendindex = 0;
            int curlyopen = 0;
            int indeOfFirstCloseCurly = subs.IndexOf('}');
            for (int st = 0; st < indeOfFirstCloseCurly; )//count opening curly brackets
            {
                int curindex = subs.IndexOf('{', st);
                if (curindex >= 0 && curindex < indeOfFirstCloseCurly)//if found before first closing '}'
                { curlyopen++; st = curindex + 1; }
                else break;
            }
            int curlyclose = 0;
            for (int st = 0; st < subs.Length - 1; )//count closing curly brackets
            {
                int curindex = subs.IndexOf('}', st);
                if (curindex >= 0)//if found
                { curlyclose++; st = curindex + 1; }
                else break;

                if (curlyclose == curlyopen)
                {
                    blockendindex = curindex;//length to be extracted
                    break;
                }
            }
            if (curlyopen != curlyclose)
            {
                MessageBox.Show(this, "Error in block declaration. Mismatch { or }");
                return "";
            }
            string tmpstr = subs.Substring(0, blockendindex + 1).Replace('\n', ';').Replace('\r', ' ').Replace(" in ", "$#in#$").Replace(" ", string.Empty).Replace("$#in#$", " in ").Trim();
            do
            {
                stmt = tmpstr.Replace(";;", ";");///.Replace("}", ";};")
            } while (stmt.Contains(";;"));
            end = blockendindex + 1;
            stmt = stmt.Replace("}", ";} ");
            return stmt;
        }

        // reading back sink file and creating & displaying output; for non-BSkyFormat commands
        private void CreateOuput(OutputWindow ow)
        {
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Started creating output.", LogLevelEnum.Info);

            //////////////////// for fetching BSkyFormat object Queue and process each object //////////////////////
            int bskyformatobjectindex = 0;
            bool bskyQFetched = false;
            CommandRequest fetchQ = null;
            string sinkfileBSkyFormatMarker = "[1] \"BSkyFormatInternalSyncFileMarker\"";
            string sinkfileBSkyGraphicFormatMarker = "[1] \"BSkyGraphicsFormatInternalSyncFileMarker\""; //09Jun2015 
            //used to maintain the sequence of print in between BSkyFormats in case of block BSkyFormat
            bool isBlockCommand = false;
            //09Jun2015 used to maintain the sequence of print in between BSkyGraphicFormats in case of block commands
            bool isBlockGraphicCommand = false;

            //for deciding when to send output to output window in case of block BSkyFormat
            //for block BSkyFormat we wait and create all different UI elements first so as to maintain sequence and then send them to output
            //for non-block BSkFormat we send immediately after execution. No stacking up of UI elements ( AUXGrid, AUPara etc..)
            bool isBlock = false;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            //if (true) return;
            CommandOutput lst = new CommandOutput(); ////one analysis////////
            CommandOutput grplst = new CommandOutput();//21Nov2013 Separate for Graphic. So Parent node name will be R-Graphic
            lst.IsFromSyntaxEditor = true;//lst belongs to Syn Editor
            if (saveoutput.IsChecked == true)//10Jan2013
                lst.SelectedForDump = true;
            XmlDocument xd = null;
            //string auparas = "";
            StringBuilder sbauparas = new StringBuilder("");
            //////////////// Read output ans message from file and create output then display /////
            //// read line by line  /////
            string sinkfilefullpathname = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("tempsink"));//23nov2012
            // load default value if no path is set or invalid path is set
            if (sinkfilefullpathname.Trim().Length == 0 || !IsValidFullPathFilename(sinkfilefullpathname, true))
            {
                MessageBox.Show(this, "Key 'tempsink' not found in config file. Aborting...");
                return;
            }
            System.IO.StreamReader file = new System.IO.StreamReader(sinkfilefullpathname);// OpenSinkFile(@sinkfilefullpathname, "rt");
            object linetext = null; string line;
            bool insideblock = false;//20May2014
            bool readSinkFile = true;
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Started reading sink", LogLevelEnum.Info);
            //int imgcount = GraphicDeviceImageCounter;//couter that keeps track of how many images already got processed. Helps in creating a image filename.
            while ((line = file.ReadLine()) != null)//(readSinkFile)
            {
                {
                    linetext = line;
                }

                if (linetext == null || linetext.ToString().Equals("EOF"))
                {
                    break;
                }
                if (linetext != null && linetext.Equals("NULL") && lastcommandwasgraphic)//27May2015 to supress NULL for some listed graphic commands
                {
                    continue;
                }
                if (linetext.ToString().Trim().Contains(sinkfileBSkyFormatMarker))//Contains("[1] \"BSkyFormat\"")) //14Jun2014 if it is BSkyFormat in block (read from sink file)
                {
                    isBlockCommand = true;
                }
                else if (linetext.ToString().Trim().Contains(sinkfileBSkyGraphicFormatMarker))//Contains("[1] \"BSkyGraphicsFormat\"")) //09Jun2015 if it is BSkyGraphicsFormat in block (read from sink file)
                {
                    isBlockGraphicCommand = true;
                }
                else
                {
                    isBlockCommand = false;
                }
                //////// create XML doc /////////
                if (linetext != null)//06May2013 we need formatting so we print blank lines.. && linetext.ToString().Length > 0)
                {
                    /////// Trying to extract command from print //////
                    string commnd = linetext.ToString();
                    int opncurly = commnd.IndexOf("{");
                    int closcurly = commnd.IndexOf("}");
                    int lencommnd = closcurly - opncurly - 1;
                    if (opncurly != -1 && closcurly != -1)
                        commnd = commnd.Substring(opncurly + 1, lencommnd);//could be graphic or BSkyFormat in sink file.
                    if (false)//11Aug2015 fix for BSkyFormat not printed if inside R function. if (commnd.Contains("BSkyFormat("))//09Jun2015 || isGraphicCommand(_command)) // is BSKyFormat or isGraphic Command
                    {
                        SendToOutput(sbauparas.ToString(), ref lst, ow);//22May2014
                        sbauparas.Clear();
                    }
                    else if (isBlockCommand)//14Jun2014 for Block BSkyFormat.
                    {
                        if (sbauparas.Length > 0)
                        {
                            createAUPara(sbauparas.ToString(), lst);//Create & Add AUPara to lst 
                            sbauparas.Clear();
                        }
                    }
                    else
                    {
                        if (sbauparas.Length < 1)
                        {
                            sbauparas.Append(linetext.ToString());//First Line of AUPara. Without \n
                            if (sbauparas.ToString().Trim().IndexOf("BSkyFormat(") == 0)//21Nov2013
                                lst.NameOfAnalysis = "BSkyFormat-Command";
                        }
                        else
                        {
                            //auparas = auparas.Replace("<", "&lt;") + "\n" + linetext.ToString();//all lines separated by new line
                            sbauparas.Append("\n" + linetext.ToString());//all lines separated by new line
                        }
                    }



                    ////for graphics////   //09Jun2015 This whole 'if' may not be needed
                    if (false)
                    {
                        SendToOutput(commnd, ref lst, ow);
                        //////////// Here is new code///////20May2014
                        CommandRequest grpcmd = new CommandRequest();
                        CloseGraphicsDevice();
                        OpenGraphicsDevice();//05May2013
                        grpcmd.CommandSyntax = commnd;// linetext.ToString();
                        analytics.ExecuteR(grpcmd, false, false);
                        CloseGraphicsDevice();
                        insideblock = true;
                        //////////////////////////////////////////////////////////////////////////////////
                        //// add auparas first to lst to maintain order///
                        if (sbauparas.Length > 0)
                        {
                            createAUPara(sbauparas.ToString(), lst);//Create & Add AUPara to lst and empty dommid
                            sbauparas.Clear();
                        }
                        ////// now add image to lst ////
                        string synedtimg = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("sinkimage"));//23nov2012
                        /////03May2013  Create zero padding string //// %03d means 000,  %04d means 0000
                        int percentindex = synedtimg.IndexOf("%");
                        int dindex = synedtimg.IndexOf("d", percentindex);
                        string percentstr = synedtimg.Substring(percentindex, (dindex - percentindex + 1));
                        string numbr = synedtimg.Substring(percentindex + 1, (dindex - percentindex - 1));
                        int zerocount = Convert.ToInt16(numbr);

                        string zeropadding = string.Empty;
                        for (int zeros = 1; zeros <= zerocount; zeros++)
                        {
                            zeropadding = zeropadding + "0";
                        }
                        int img_count = 0;//number of images to load in output
                        for (; ; )//03May2013 earlier there was no for loop for following code
                        {
                            img_count++;
                            string tempsynedtimg = synedtimg.Replace(percentstr, img_count.ToString(zeropadding));
                            // load default value if no path is set or invalid path is set
                            if (tempsynedtimg.Trim().Length == 0 || !IsValidFullPathFilename(tempsynedtimg, true))
                            {
                                break;
                            }
                            string source = @tempsynedtimg;
                            long imgsize = new FileInfo(synedtimg).Length;//find size of the imagefile
                            Image myImage = new Image();
                            ///////////RequestCachePolicy uriCachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheOnly);
                            var bitmap = new BitmapImage();
                            try
                            {
                                var stream = File.OpenRead(source);
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = stream;
                                bitmap.EndInit();
                                stream.Close();
                                stream.Dispose();
                                myImage.Source = bitmap;
                                bitmap.StreamSource.Close(); //trying to close stream 03Feb2014

                                if (isBlockCommand)
                                    createBSkyGraphic(myImage, lst); //20May2014 If graphic is inside block or loop
                                else
                                    createBSkyGraphic(myImage, grplst); //if graphic is outside block or loop
                                DeleteFileIfPossible(@tempsynedtimg);
                            }
                            catch (Exception ex)
                            {
                                logService.WriteToLogLevel("Error reading Image file " + source + "\n" + ex.Message, LogLevelEnum.Error);
                                MessageBox.Show(this, ex.Message);
                            }

                        }
                        if (img_count < 1) ////03May2013 if no images were added to output lst. then return.
                        {
                            return;
                        }

                    }
                    if (isBlockGraphicCommand)//for block graphics //09Jun2015
                    {
                        CloseGraphicsDevice();
                        insideblock = true;

                        ////// now add image to lst ////
                        string synedtimg = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("sinkimage"));//23nov2012
                        /////03May2013  Create zero padding string //// %03d means 000,  %04d means 0000
                        int percentindex = synedtimg.IndexOf("%");
                        int dindex = synedtimg.IndexOf("d", percentindex);
                        string percentstr = synedtimg.Substring(percentindex, (dindex - percentindex + 1));
                        string numbr = synedtimg.Substring(percentindex + 1, (dindex - percentindex - 1));
                        int zerocount = Convert.ToInt16(numbr);

                        string zeropadding = string.Empty;
                        for (int zeros = 1; zeros <= zerocount; zeros++)
                        {
                            zeropadding = zeropadding + "0";
                        }

                        {
                            GraphicDeviceImageCounter++;//imgcount++;
                            string tempsynedtimg = synedtimg.Replace(percentstr, GraphicDeviceImageCounter.ToString(zeropadding));
                            // load default value if no path is set or invalid path is set
                            if (tempsynedtimg.Trim().Length == 0 || !IsValidFullPathFilename(tempsynedtimg, true))
                            {

                                isBlockGraphicCommand = false; //09Jun2015 reset, as we dont know what next command is. May or may not be graphic marker
                                // not needed if one graphic for one graphic marker imgcount--;
                                break;
                            }
                            string source = @tempsynedtimg;

                            Image myImage = new Image();

                            var bitmap = new BitmapImage();
                            try
                            {
                                var stream = File.OpenRead(source);
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = stream;
                                bitmap.EndInit();
                                stream.Close();
                                stream.Dispose();
                                myImage.Source = bitmap;
                                bitmap.StreamSource.Close(); //trying to close stream 03Feb2014

                                if (isBlockGraphicCommand)
                                    createBSkyGraphic(myImage, lst); //20May2014 If graphic is inside block or loop
                                else
                                    createBSkyGraphic(myImage, grplst); //if graphic is outside block or loop
                                DeleteFileIfPossible(@tempsynedtimg);
                            }
                            catch (Exception ex)
                            {
                                logService.WriteToLogLevel("Error reading Image file " + source + "\n" + ex.Message, LogLevelEnum.Error);
                                MessageBox.Show(this, ex.Message);
                            }

                        }
                        if (GraphicDeviceImageCounter < 1) ////03May2013 if no images were added to output lst. then return.
                        {
                            sbauparas.Clear();//resetting
                            isBlockGraphicCommand = false;
                            return;
                        }
                        sbauparas.Clear();//resetting
                        isBlockGraphicCommand = false;
                    }
                    else if (isBlockCommand)// (linetext.ToString().Trim().Contains("[1] \"BSkyFormat\""))//21may2014
                    {
                        int bskyfrmtobjcount = 0;
                        if (!bskyQFetched)
                        {
                            fetchQ = new CommandRequest();
                            fetchQ.CommandSyntax = "BSkyQueue = BSkyGetHoldFormatObjList()";// Fetch Queue object
                            analytics.ExecuteR(fetchQ, false, false);

                            fetchQ.CommandSyntax = "is.null(BSkyQueue)";// check if Queue is null
                            object o = analytics.ExecuteR(fetchQ, true, false);//return false or true
                            if (o.ToString().ToLower().Equals("false"))//Queue has elements
                            {
                                bskyQFetched = true;
                            }
                        }
                        if (bskyQFetched)
                        {
                            bskyformatobjectindex++;
                            commnd = "BSkyFormat(BSkyQueue[[" + bskyformatobjectindex + "]])";

                            ExecuteSinkBSkyFormatCommand(commnd, ref bskyfrmtobjcount, lst);
                            lst = new CommandOutput();//"Child already has parent" error, fix
                            isBlock = true;
                        }
                        isBlockCommand = false;//09Jun2015 next command may or may not be BSkyFormat marker.
                    }
                }//if linetext!null
            }//while EOF sink file
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Finished reading sink", LogLevelEnum.Info);
            file.Close(); //CloseSinkFile();
            SendToOutput(sbauparas.ToString(), ref lst, ow, isBlock);//send output to window or disk file
            SendToOutput(null, ref grplst, ow, isBlock);//21Nov2013. separate node for graphic
            if (lst != null && lst.Count > 0 && isBlock) // Exceutes when there is block command
            {
                sessionlst.Add(lst);//15Nov2013
                lst = new CommandOutput();//after adding to session new object is allocated for futher output creation
            }

            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Finished creating output.", LogLevelEnum.Info);
        }

        private void CreateAllGraphicOutput(OutputWindow ow)
        {
            CommandOutput grplst = new CommandOutput();
            long EmptyImgSize = EMPTYIMAGESIZE;//size(in bytes) of empty png file
            CloseGraphicsDevice();
            ////// now add image to lst ////
            string synedtimg = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("sinkimage"));//23nov2012
            /////03May2013  Create zero padding string //// %03d means 000,  %04d means 0000
            int percentindex = synedtimg.IndexOf("%");
            int dindex = synedtimg.IndexOf("d", percentindex);
            string percentstr = synedtimg.Substring(percentindex, (dindex - percentindex + 1));
            string numbr = synedtimg.Substring(percentindex + 1, (dindex - percentindex - 1));
            int zerocount = Convert.ToInt16(numbr);

            string zeropadding = string.Empty;
            for (int zeros = 1; zeros <= zerocount; zeros++)
            {
                zeropadding = zeropadding + "0";
            }
            int imgcount = GraphicDeviceImageCounter;//number of images to load in output
            for (; ; )//03May2013 earlier there was no for loop for following code
            {
                imgcount++;
                string tempsynedtimg = synedtimg.Replace(percentstr, imgcount.ToString(zeropadding));
                // load default value if no path is set or invalid path is set
                if (tempsynedtimg.Trim().Length == 0 || !IsValidFullPathFilename(tempsynedtimg, true))
                {
                    break;
                }
                string source = @tempsynedtimg;
                long imgsize = new FileInfo(source).Length;//find size of the imagefile
                if (imgsize > EmptyImgSize)//if image is not an empty image
                {
                    Image myImage = new Image();

                    var bitmap = new BitmapImage();
                    try
                    {
                        var stream = File.OpenRead(source);
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        stream.Close();
                        stream.Dispose();
                        myImage.Source = bitmap;
                        bitmap.StreamSource.Close(); //trying to close stream 03Feb2014

                        createBSkyGraphic(myImage, grplst); //add graphic
                        DeleteFileIfPossible(@tempsynedtimg);
                    }
                    catch (Exception ex)
                    {
                        logService.WriteToLogLevel("Error reading Image file " + source + "\n" + ex.Message, LogLevelEnum.Error);
                        MessageBox.Show(this, ex.Message);
                    }

                }

            }
            if (imgcount < 1) ////03May2013 if no images were added to output lst. then return.
            {
                return;
            }
            SendToOutput(null, ref grplst, ow, false);//send all graphic to output
            OpenGraphicsDevice();//in case of errors or no errors, you must open graphic device
        }

        /// <summary>
        /// Deletes any file. But its used for deleting temporaray image files left after any graphics command is executed.
        /// And this delte operation runs just before opening PNG graphics device.
        /// </summary>
        /// <param name="fulpathfilename"></param>
        private void DeleteFileIfPossible(string fulpathfilename)
        {
            try
            {
                File.Delete(fulpathfilename);
            }
            catch (IOException)
            {
                logService.Error("Unable to delete :" + fulpathfilename);
            }
        }


        private void SendToOutput(string auparas, ref CommandOutput lst, OutputWindow ow, bool isBlockCommand = false)//, bool last=false)
        {
            if (auparas != null && auparas.Trim().Length > 0)
            {
                this.createAUPara(auparas, lst);//Create & Add AUPara to lst and empty dommid
                auparas = null;

            }
            if (lst != null && lst.Count > 0 && !isBlockCommand) //if non block command, then sent to output
            {
                sessionlst.Add(lst);//15Nov2013
                lst = new CommandOutput();//after adding to session new object is allocated for futher output creation
                //15Nov2013 ow.AddAnalyisFromSyntaxEditor(lst);//, last);/// send to output and/or dump file
            }
        }

        private void SetSink() // set desired sink
        {
            sinkcmd.CommandSyntax = "options('warn'=1)";// trying to flush old errors
            analytics.ExecuteR(sinkcmd, false, false);

            sinkcmd.CommandSyntax = "sink(fp, append=FALSE, type=c(\"output\"))";// command
            analytics.ExecuteR(sinkcmd, false, false);
            sinkcmd.CommandSyntax = "sink(fp, append=FALSE, type=c(\"message\"))";// command
            analytics.ExecuteR(sinkcmd, false, false);
        }

        private void ResetSink() // reset to original
        {
            sinkcmd.CommandSyntax = "sink(stderr(), type=c(\"message\"))";// command
            analytics.ExecuteR(sinkcmd, false, false);
            sinkcmd.CommandSyntax = "sink()";//stdout(), type=\"output\")";// command
            analytics.ExecuteR(sinkcmd, false, false);
        }

        private void OpenSinkFile(string fullpathfilename, string mode)
        {
            string unixstylepath = fullpathfilename.Replace("\\", "/");
            sinkcmd.CommandSyntax = "fp<- file(\"" + unixstylepath + "\", open=\"" + mode + "\")";// command
            analytics.ExecuteR(sinkcmd, false, false);
        }

        private void CloseSinkFile()
        {
            sinkcmd.CommandSyntax = "flush(fp)";// command
            analytics.ExecuteR(sinkcmd, false, false);
            sinkcmd.CommandSyntax = "close(fp)";// command
            analytics.ExecuteR(sinkcmd, false, false);
        }

        private string findHeaderName(string bskyformatcmd)
        {
            if (bskyformatcmd.Trim().Contains("data.frame"))
                return "data.frame";
            else if (bskyformatcmd.Trim().Contains("array"))
                return "array";
            else if (bskyformatcmd.Trim().Contains("matrix"))
                return "matrix";
            else
            {
                return (bskyformatcmd);
            }
        }

        private bool IsAnalyticsCommand(string command)
        {
            bool bskycomm = false;
            return bskycomm;
        }

        private void ExecuteSplit(string stmt)
        {
            //object o = null;
            CommandRequest cmd = new CommandRequest();

            CommandExecutionHelper ceh = new CommandExecutionHelper();
            UAMenuCommand uamc = new UAMenuCommand();
            uamc.bskycommand = stmt;
            uamc.commandtype = stmt;
            cmd.CommandSyntax = stmt;
            ceh.ExecuteSplit(stmt, FElement);
            ceh = null;
        }

        private void ExecuteBSkyCommand(string stmt)
        {
            CommandRequest cmd = new CommandRequest();
            if (IsAnalyticsCommand(stmt))
            {
                ResetSink();
                cmd.CommandSyntax = stmt;// command 
                object o = analytics.ExecuteR(cmd, false, false);//executing syntax editor commands
                SetSink();
            }
        }

        private void ExecuteBSkyFormatCommand(string stmt, ref int bskyfrmtobjcount, OutputWindow ow)
        {
            KillTempBSkyFormatObj("bskytempvarname");
            KillTempBSkyFormatObj("bskyfrmtobj");

            string originalCommand = stmt;
            CommandOutput lst = new CommandOutput(); ////one analysis////////
            lst.IsFromSyntaxEditor = true;//lst belongs to Syn Editor
            if (saveoutput.IsChecked == true)//10Jan2013
                lst.SelectedForDump = true;

            object o;
            CommandRequest cmd = new CommandRequest();
            /// find argument passed in BSkyFormat(argu) and var name  ////
            /// eg.. BSkyFormat(osmt <-one.smt.t.test(....) )
            /// subcomm will be : osmt<-one.smt.t.test(....) 
            /// varname will be : osmt
            string subcomm = string.Empty, varname = string.Empty, BSkyLeftVar = string.Empty, headername = string.Empty;
            string firstparam = string.Empty, restparams = string.Empty, leftvarname = string.Empty;//23Sep2014
            string userpassedtitle = string.Empty;
            //SplitBSkyFormat(stmt, out subcomm, out varname, out BSkyLeftVar);
            SplitBSkyFormatParams(stmt, out firstparam, out restparams, out userpassedtitle);//23Spe2014
            if (userpassedtitle.Trim().Length > 0)//user passed title has the highest priority
            {
                headername = userpassedtitle.Trim();
            }
            
            {
                //23Sep2014 if firstParam is of the type obj<-OSMT(...) OR obj<-obj2
                if (firstparam.Contains("<-") || firstparam.Contains("=")) //if it has assignment
                {
                    int idxassign=-1, idxopenbrket=-1;
                    if(firstparam.Contains("("))// if obj<-OSMT(...)
                    {
                        idxopenbrket = firstparam.IndexOf("(");
                        string firsthalf = firstparam.Substring(0,idxopenbrket);// "obj <- OSMT("
                        idxassign = firsthalf.IndexOf("<-");
                        if (idxassign == -1)// '<-' not present(found in half)
                            idxassign = firsthalf.IndexOf("=");
                    }

                    if (idxassign > -1 && idxopenbrket > -1 && idxopenbrket > idxassign)
                    {
                        leftvarname = firstparam.Substring(0, idxassign);
                        headername = leftvarname.Trim();
                        cmd.CommandSyntax = firstparam;// command: osmt<-one.smt.tt(...)
                        o = analytics.ExecuteR(cmd, false, false);//executing sub-command; osmt<-one.smt.tt(...)

                    }
                    else if (idxopenbrket < 0 )//type obj <- obj2
                    {
                        idxassign = firstparam.IndexOf("<-");
                        if (idxassign == -1)// '<-' not present
                            idxassign = firstparam.IndexOf("=");
                        if (idxassign > -1)//if assignment is there
                        {
                            leftvarname = firstparam.Substring(0, idxassign);
                            headername = leftvarname.Trim();
                            cmd.CommandSyntax = firstparam;// command: osmt<-one.smt.tt(...)
                            o = analytics.ExecuteR(cmd, false, false);//executing sub-command; osmt<-one.smt.tt(...)
                        }
                    }
                }

                /////25Feb2013 for writing errors in OutputWindow////
                string sinkfilefullpathname = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("tempsink")); //23nov2012
                // load default value if no path is set or invalid path is set
                if (sinkfilefullpathname.Trim().Length == 0 || !IsValidFullPathFilename(sinkfilefullpathname, false))
                {
                    MessageBox.Show(this, "Key 'tempsink' not found in config file. Aborting...");
                    return; //return type was void before 22May2014
                }
                OpenSinkFile(@sinkfilefullpathname, "wt"); //06sep2012
                SetSink(); //06sep2012

                ////////////////////////////////////////////////////////////////////////
                //13Aug2012 headername = findHeaderName(subcomm); // data.frame / matrix / array header 
                varname = "bskytempvarname";
                KillTempBSkyFormatObj(varname);

                //Now run command
                firstparam = (leftvarname.Trim().Length>0? leftvarname : firstparam);
                //23Sep2014 cmd.CommandSyntax = varname + " <- " + subcomm;// command: varname <- one.smt.tt(...)
                cmd.CommandSyntax = varname + " <- " + firstparam;// varname <- obj OR OSMT()
                o = analytics.ExecuteR(cmd, false, false);//executing sub-command
                ////////////////////////////////////////////////////////////////////////

                /////25Feb2013 for writing errors in OutputWindow////
                ResetSink();
                CloseSinkFile();
                CreateOuput(ow);
            }

            //if var does not exists then there could be error in command execution.
            cmd.CommandSyntax = "exists('" + varname + "')";
            o = analytics.ExecuteR(cmd, true, false);
            if (o.ToString().ToLower().Equals("false"))//possibly some error occured
            {
                string ewmessage = "Object cannot be formatted using BSKyFormat. Object: " + firstparam + ", does not exists.";
                //if (ow != null)//22May2014
                SendErrorToOutput(originalCommand + "\n" + ewmessage, ow); //03Jul2013
                return; //return type was void before 22May2014
            }

            //Check if varname is null
            cmd.CommandSyntax = "is.null(" + varname + ")";
            o = analytics.ExecuteR(cmd, true, false);
            if (o.ToString().ToLower().Equals("true"))//possibly some error occured
            {
                string ewmessage = "Object cannot be formatted using BSKyFormat. Object: " + firstparam + ", is null.";
                SendErrorToOutput(originalCommand + "\n" + ewmessage, ow); //03Jul2013
                return; //return type was void before 22May2014
            }


            //setting up flag for showing default ("1","2","3" )row headers.
            //This will not work if BSkyReturnStructure is returned(in varname).
            bsky_no_row_header = false;
            cmd.CommandSyntax = "is.null(row.names(" + varname + ")[1])";
            o = analytics.ExecuteR(cmd, true, false);
            if (o.ToString().ToLower().Equals("false"))//row name at [1] exists
            {
                cmd.CommandSyntax = "row.names(" + varname + ")[1]";
                o = analytics.ExecuteR(cmd, true, false);
                if (o.ToString().Trim().ToLower().Equals("bsky_no_row_header"))
                {
                    bsky_no_row_header = true;
                }
            }

            //one mandatory parameter
            string mandatoryparamone = ", bSkyFormatAppRequest = TRUE";
            if (restparams.Trim().Length > 0 && restparams.Trim().Contains("bSkyFormatAppRequest"))//if parameter is already present, no need to add it.
            {
                mandatoryparamone = string.Empty;
            }

            //second mandatory parameter
            string mandatoryparamtwo = ", singleTableOutputHeader = '" + headername + "'"; //   \"c(\\\"a\\\")\" )"
            if (restparams.Trim().Length > 0 && restparams.Trim().Contains("singleTableOutputHeader"))//if parameter is already present, no need to add it.
            {
                mandatoryparamtwo = string.Empty;
            }

            //create BSkyFormat command for execution and execute
            if(restparams.Trim().Length > 0)
                stmt = "BSkyFormat(" + varname + mandatoryparamone + mandatoryparamtwo +","+restparams+")";//stmt = "BSkyFormat(" + varname + ")";
            else
                stmt = "BSkyFormat(" + varname + mandatoryparamone + mandatoryparamtwo+" )";

            stmt = BSkyLeftVar + stmt;// command is BSkyLeftVar <- BSkyFormat(varname)
            /// BSkyLeftVar <- can be blank if user has no assigned leftvar to BSkyFormat

            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Command reconstructed : " + stmt, LogLevelEnum.Info);

            string objclass = "", objectname = "";

            if (stmt.Contains("BSkyFormat("))// Array, Matrix, Data Frame or BSkyObject(ie..Analytic commands)
            {
                bskyfrmtobjcount++;
                stmt = "bskyfrmtobj <- " + stmt; // BSkyFormat has BSkyFormat2 call inside of it
                objectname = "bskyfrmtobj";// +bskyfrmtobjcount.ToString();
                cmd.CommandSyntax = stmt;// command 
                o = analytics.ExecuteR(cmd, false, false);//executing BSkyFormat

                ///Check if returned object is null
                cmd.CommandSyntax = "is.null(" + objectname + ")";
                o = analytics.ExecuteR(cmd, true, false);
                if (o.ToString().ToLower().Equals("true"))//possibly some error occured
                {
                    string ewmessage = "Object cannot be formatted using BSKyFormat. Type not supported. Supported types are :\n array, matrix dataframe and BSky return structure.";
                    SendErrorToOutput(originalCommand + "\n" + ewmessage, ow); //03Jul2013
                    return; //return type was void before 22May2014
                }

                #region Generate UI for data.frame/ matrix / array and analytics commands
                if (BSkyLeftVar.Trim().Length < 1) // if left var does not exists then generate UI tables
                {
                    lst.NameOfAnalysis = originalCommand.Contains("BSkyFormat") ? "BSkyFormat-Command" : originalCommand;
                    //cmd.CommandSyntax = "class(bskyfrmtobj" + bskyfrmtobjcount.ToString() + ")";
                    cmd.CommandSyntax = "class(bskyfrmtobj)";
                    objclass = (string)analytics.ExecuteR(cmd, true, false);

                    if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: BSkyFormat object type : " + objclass, LogLevelEnum.Info);

                    if (objclass.ToString().ToLower().Equals("data.frame") || objclass.ToString().ToLower().Equals("matrix") || objclass.ToString().ToLower().Equals("array"))
                    {
                        if (headername != null && headername.Trim().Length < 1) //06May2014
                            headername = subcomm;
                        if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: BSkyFormatting DF/Matrix/Arr : " + objclass, LogLevelEnum.Info);
                        BSkyFormatDFMtxArr(lst, objectname, headername, ow);
                    }
                    else if (objclass.ToString().ToLower().Equals("list"))//BSkyObject returns "list"
                    {
                        if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: BSkyFormatting : " + objclass, LogLevelEnum.Info);
                        SendToOutput("", ref lst, ow);
                        ///tetsing whole else if
                        objectname = "bskyfrmtobj";// +bskyfrmtobjcount.ToString();
                        
                        cmd.CommandSyntax = "is.null(" + objectname + "$BSkySplit)";//$BSkySplit or $split in return structure
                        if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Executing : " + cmd.CommandSyntax, LogLevelEnum.Info);
                        bool isNonBSkyList1=false;
                        object isNonBSkyList1str = analytics.ExecuteR(cmd, true, false);
                        if (isNonBSkyList1str != null && isNonBSkyList1str.ToString().ToLower().Equals("true"))
                        {
                            isNonBSkyList1 = true;
                        }
                        cmd.CommandSyntax = "is.null(" + objectname + "$list2name)";//another type pf BSky list
                        if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Executing : " + cmd.CommandSyntax, LogLevelEnum.Info);
                        bool isNonBSkyList2=false ;
                        object isNonBSkyList2str = analytics.ExecuteR(cmd, true, false);
                        if (isNonBSkyList2str != null && isNonBSkyList2str.ToString().ToLower().Equals("true"))
                        {
                            isNonBSkyList2 = true;
                        }

                        if (!isNonBSkyList1)
                        {
                            //if there was error in execution, say because non scale field has scale variable
                            // so now if we first check if $executionstatus = -2, we know that some error has occured.
                            cmd.CommandSyntax = objectname + "$executionstatus";//$BSkySplit or $split in return structure
                            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Executing : " + cmd.CommandSyntax, LogLevelEnum.Info);
                            object errstat = analytics.ExecuteR(cmd, true, false);
                            if (errstat != null && (errstat.ToString().ToLower().Equals("-2") || errstat.ToString().ToLower().Equals("-1")))
                            {
                                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Execution Stat : " + errstat, LogLevelEnum.Info);
                                if(errstat.ToString().ToLower().Equals("-2"))
                                    SendErrorToOutput("Critical Error Occurred!", ow);//15Jan2015
                                else
                                    SendErrorToOutput("Error Occurred!", ow);//03Jul2013
                            }

                            cmd.CommandSyntax = objectname + "$nooftables";//$nooftables=0, means no data to display. Not even error warning tables are there.
                            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Executing : " + cmd.CommandSyntax, LogLevelEnum.Info);
                            object retval = analytics.ExecuteR(cmd, true, false);
                            if (retval != null && retval.ToString().ToLower().Equals("0"))
                            {
                                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: No of Tables : " + retval, LogLevelEnum.Info);
                                SendErrorToOutput("No tables to show in output!", ow);//03Jul2013
                            }

                            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Start creating actual UI tables : ", LogLevelEnum.Info);
                            //finally we can now format the tables of BSkyReturnStruture list
                            RefreshOutputORDataset(objectname, cmd, originalCommand, ow); //list like one sample etc..
                        }
                        else if (!isNonBSkyList2)
                        {
                            //if (ow != null)//22May2014
                            FormatBSkyList2(lst, objectname, headername, ow); //list that contains tables 2nd location onwards
                        }
                    }
                    else // invalid format
                    {
                        /// put it in right place
                        string ewmessage = "This Object cannot be formatted using BSKyFormat. BSkyFormat can be used on Array, Matrix, Data Frame and BSky List objects only";
                        //if (ow != null)//22May2014
                        SendErrorToOutput(originalCommand + "\n" + ewmessage, ow);//03Jul2013
                    }
                }/// if leftvar is not assigned generate UI
                #endregion
            }
            return;//22May2014
        }

        //30Sep2014
        // This var must be removed from R memory otherwise when next BSkyFormat fails, it actually formats older object thinking that it is the latest one.
        //To remove temp variable that holds BSkyFormat object.
        private void KillTempBSkyFormatObj(string varname)
        {
            object o;
            CommandRequest cmd = new CommandRequest();
            cmd.CommandSyntax = "exists('" + varname + "')";//removing var so that old obj from last session is not present.
            o = analytics.ExecuteR(cmd, true, false);
            if (o.ToString().ToLower().Equals("true")) // if found, remove it
            {
                cmd.CommandSyntax = "rm('" + varname + "')";//removing var so that old obj from last session is not present.
                o = analytics.ExecuteR(cmd, false, false);
            }
        }

        private void ExecuteSinkBSkyFormatCommand(string stmt, ref int bskyfrmtobjcount, CommandOutput lst)
        {

            string originalCommand = stmt;
            //CommandOutput lst = new CommandOutput(); ////one analysis////////
            lst.IsFromSyntaxEditor = true;//lst belongs to Syn Editor
            if (saveoutput.IsChecked == true)//10Jan2013
                lst.SelectedForDump = true;
            object o;
            CommandRequest cmd = new CommandRequest();
            /// find argument passed in BSkyFormat(argu) and var name  ////
            /// eg.. BSkyFormat(osmt <-one.smt.t.test(....) )
            /// subcomm will be : osmt<-one.smt.t.test(....) 
            /// varname will be : osmt
            string subcomm = string.Empty, varname = string.Empty, BSkyLeftVar = string.Empty, headername = string.Empty;
            SplitBSkyFormat(stmt, out subcomm, out varname, out BSkyLeftVar);

            if (BSkyLeftVar.Trim().Length > 0) // if left var exists
            {
                BSkyLeftVar = BSkyLeftVar + " <- "; // so that BSkyLeftVar <- BSkyFormat(...) )
            }
            ////now execute subcomm first then pass varname in BSkyFormat(varname)
            if (varname.Length > 0)//will be > 0 only for BSkyFormat(osmt<-one.smt.tt(...) ) OR for BSkyFormat(df)
            {
                headername = varname.Trim();
                cmd.CommandSyntax = subcomm;// command: osmt<-one.smt.tt(...)
                if (!varname.Equals(subcomm))
                    o = analytics.ExecuteR(cmd, false, false);//executing sub-command; osmt<-one.smt.tt(...)
            }
            else //for BSkyFormat(one.smt.tt(...) )
            {
                /////25Feb2013 for writing errors in OutputWindow////
                string sinkfilefullpathname = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("tempsink"));//23nov2012
                // load default value if no path is set or invalid path is set
                if (sinkfilefullpathname.Trim().Length == 0 || !IsValidFullPathFilename(sinkfilefullpathname, false))
                {
                    MessageBox.Show(this, "Key 'tempsink' not found in config file. Aborting...");
                    return;
                }

                ////////////////////////////////////////////////////////////////////////
                //13Aug2012 headername = findHeaderName(subcomm); // data.frame / matrix / array header 
                varname = "bskytempvarname";
                //Find if bskytempvarname already exists. If it exists then remove from memory
                cmd.CommandSyntax = "exists('" + varname + "')";//removing var so that old obj from last session is not present.
                o = analytics.ExecuteR(cmd, true, false);
                if (o.ToString().ToLower().Equals("true")) // if found, remove it
                {
                    cmd.CommandSyntax = "rm('" + varname + "')";//removing var so that old obj from last session is not present.
                    o = analytics.ExecuteR(cmd, false, false);
                }

                //Now run command
                cmd.CommandSyntax = varname + " <- " + subcomm;// command: varname <- one.smt.tt(...)
                o = analytics.ExecuteR(cmd, false, false);//executing sub-command
                ////////////////////////////////////////////////////////////////////////
            }
            //if var does not exists then there could be error in command execution.
            cmd.CommandSyntax = "exists('" + varname + "')";
            o = analytics.ExecuteR(cmd, true, false);
            if (o.ToString().ToLower().Equals("false"))//possibly some error occured
            {
                string ewmessage = "Object cannot be formatted using BSKyFormat. Object: " + varname + ", does not exists.";
                SendErrorToOutput(originalCommand + "\n" + ewmessage, ow); //03Jul2013
                return;
            }

            //Added extra parameter with TRUE value to fix problem related to uadatasets.sk$holdBSkyFormatObject getting back objeects.
            // when cmd executes in R, few line below. "analytics.ExecuteR(c..."
            stmt = "BSkyFormat(" + varname + ", bSkyFormatAppRequest = TRUE)";

            stmt = BSkyLeftVar + stmt;// command is BSkyLeftVar <- BSkyFormat(varname)
            /// BSkyLeftVar <- can be blank if user has no assigned leftvar to BSkyFormat

            string objclass = "", objectname = "";

            if (stmt.Contains("BSkyFormat("))// Array, Matrix, Data Frame or BSkyObject(ie..Analytic commands)
            {
                bskyfrmtobjcount++;
                stmt = "bskyfrmtobj <- " + stmt;// +"$tables[[1]][[1]]";// +"$additional";//return value additional
                objectname = "bskyfrmtobj";// +bskyfrmtobjcount.ToString();
                cmd.CommandSyntax = stmt;// command 

                //Following statement brings back the objects of uadatasets.sk$holdBSkyFormatObject
                o = analytics.ExecuteR(cmd, false, false);//executing syntax editor commands

                #region Generate UI for data.frame/ matrix / array and analytics commands
                if (BSkyLeftVar.Trim().Length < 1) // if left var does not exists then generate UI tables
                {
                    lst.NameOfAnalysis = originalCommand.Contains("BSkyFormat") ? "BSkyFormat-Command" : originalCommand;
                    //cmd.CommandSyntax = "class(bskyfrmtobj" + bskyfrmtobjcount.ToString() + ")";
                    cmd.CommandSyntax = "class(bskyfrmtobj)";
                    objclass = (string)analytics.ExecuteR(cmd, true, false);


                    if (objclass.ToString().ToLower().Equals("data.frame") || objclass.ToString().ToLower().Equals("matrix") || objclass.ToString().ToLower().Equals("array"))
                    {
                        //lst.NameOfAnalysis = originalCommand;//for tree Parent node 07Aug2012
                        if (headername != null && headername.Trim().Length < 1) //06May2014
                        {
                            headername = subcomm;
                        }

                        BSkyFormatDFMtxArr(lst, objectname, headername, null);
                    }
                    else if (objclass.ToString().Equals("list"))//BSkyObject returns "list"
                    {
                        //if (ow != null)//22May2014
                        SendToOutput("", ref lst, ow);
                        ///tetsing whole else if
                        objectname = "bskyfrmtobj";// +bskyfrmtobjcount.ToString();

                        cmd.CommandSyntax = "is.null(" + objectname + "$BSkySplit)";//$BSkySplit or $split in return structure
                        bool isNonBSkyList1 = false;
                        object isNonBSkyList1str = analytics.ExecuteR(cmd, true, false);
                        if (isNonBSkyList1str != null && isNonBSkyList1str.ToString().ToLower().Equals("true"))
                        {
                            isNonBSkyList1 = true;
                        }
                        cmd.CommandSyntax = "is.null(" + objectname + "$list2name)";//another type pf BSky list
                        bool isNonBSkyList2 = false;
                        object isNonBSkyList2str = analytics.ExecuteR(cmd, true, false);
                        if (isNonBSkyList2str != null && isNonBSkyList2str.ToString().ToLower().Equals("true"))
                        {
                            isNonBSkyList2 = true;
                        }

                        /////////////////
                        if (!isNonBSkyList1)
                        {
                            RefreshOutputORDataset(objectname, cmd, originalCommand, ow); //list like one sample etc..
                        }
                        else if (!isNonBSkyList2)
                        {
                            FormatBSkyList2(lst, objectname, headername, ow); //list that contains tables 2nd location onwards
                        }
                    }
                    else // invalid format
                    {
                        /// put it in right place
                        string ewmessage = "This Object cannot be formatted using BSKyFormat. BSkyFormat can be used on Array, Matrix, Data Frame and BSky List objects only";
                        SendErrorToOutput(originalCommand + "\n" + ewmessage, ow);//03Jul2013
                    }
                }/// if leftvar is not assigned generate UI
                #endregion
            }

        }

        private bool ExecuteBSkyLoadRefreshDataframe(string stmt)//13Feb2014

        {
            CommandRequest cmd = new CommandRequest();
            int start = stmt.IndexOf('(');
            int end = stmt.IndexOf(')');

            string parameters = stmt.Substring(start + 1, end - start - 1);

            //parameters can be 1 or 2 (if 2 params, then one is bool)
            // IMPORTANT :: Assumed that max parameters are 2. One is optional bool parameter.
            string[] eachparam = parameters.Split(',');
            string dataframename = string.Empty;
            string boolparam = string.Empty;
            if (eachparam.Length==2)
            {
                //either of the two is dataframe name
                if (eachparam[1].Contains("load.dataframe") || eachparam[1].Contains("TRUE") || eachparam[1].Contains("FALSE"))
                {
                    dataframename = eachparam[0];
                    boolparam = eachparam[1];
                }
                else // if bool is passed as first parameter and dataframe name as second param.
                {
                    dataframename = eachparam[1];
                    boolparam = eachparam[0];
                }

            }
            else if (eachparam.Length == 1)//only one madatory param is passed which is dataframe name
            {
                dataframename = eachparam[0];
                boolparam = "TRUE";
            }
            /////get dataframe name
            if (dataframename.Contains("="))//dframe="Dataset1"
            {
                dataframename = dataframename.Substring(dataframename.IndexOf("=") + 1);
            }
            dataframename = dataframename.Trim();

            //09Jun2015 if dataframename is not passed that means there is no need to load/refresh dataframe
            if (dataframename.Length < 1)
            {
                return true;
            }

            ///get bool parama value
            if (boolparam.Contains("="))//dframe="Dataset1"
            {
                boolparam = boolparam.Substring(boolparam.IndexOf("=") + 1);
            }
            boolparam = boolparam.Trim();            

            if (boolparam.Contains("FALSE"))//do not refresh dataframe
            {
                return true;
            }

            cmd.CommandSyntax = "exists('" + dataframename + "')";//check if that dataset exists in memory.
            object o1 = analytics.ExecuteR(cmd, true, false);
            if (o1.ToString().ToLower().Equals("true")) // if found, check if data.frame type. then load it
            {
                cmd.CommandSyntax = "is.data.frame(" + dataframename + ")";//check if its 'data.frame' type.
                object o2 = analytics.ExecuteR(cmd, true, false);
                if (o2.ToString().ToLower().Equals("true")) // if data.frame type
                {
                    FileOpenCommand foc = new FileOpenCommand();
                    return foc.OpenDataframe(dataframename);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void ExecuteBSkyRemoveRefreshDataframe(string stmt)//20Feb2014
        {
            int start = stmt.IndexOf('(');
            int end = stmt.IndexOf(')');

            string dataframename = stmt.Substring(start + 1, end - start - 1);
            FileCloseCommand fcc = new FileCloseCommand();
            fcc.CloseDatasetFromSyntax(dataframename);
        }

        private void ExecuteXMLTemplateDefinedCommands(string stmt)//10Jul2014
        {
            CommandRequest xmlgrpcmd = new CommandRequest();

            xmlgrpcmd.CommandSyntax = stmt;
            //o = analytics.ExecuteR(xmlgrpcmd, false, false);

            UAMenuCommand uamc = new UAMenuCommand();
            uamc.bskycommand = stmt;
            uamc.commandtype = stmt;
            CommandExecutionHelper auacb = new CommandExecutionHelper();
            auacb.MenuParameter = menuParameter;
            auacb.RetVal = analytics.Execute(xmlgrpcmd);
            auacb.ExecuteXMLDefinedDialog(stmt);
        }
        //pulled out from ExecuteBSkyFromatCommand() method above. For BskyFormat DataFrame Matrix Array
        private void BSkyFormatDFMtxArr(CommandOutput lst, string objectname, string headername, OutputWindow ow)
        {
            CommandRequest cmddf = new CommandRequest();
            int dimrow = 1, dimcol = 1;
            bool rowexists = false, colexists = false;
            string dataclassname = string.Empty;

            //Find class of data passed. data.frame, matrix, or array
            cmddf.CommandSyntax = "class(" + objectname + ")"; // Row exists
            object retres = analytics.ExecuteR(cmddf, true, false);
            if (retres != null)
                dataclassname = retres.ToString();

            //find if dimension exists
            cmddf.CommandSyntax = "!is.na(dim(" + objectname + ")[1])"; // Row exists
            retres = analytics.ExecuteR(cmddf, true, false);
            if (retres != null && retres.ToString().ToLower().Equals("true"))
                rowexists = true;
            cmddf.CommandSyntax = "!is.na(dim(" + objectname + ")[2])";// Col exists
            retres = analytics.ExecuteR(cmddf, true, false);
            if (retres != null && retres.ToString().ToLower().Equals("true"))
                colexists = true;
            /// Find size of matrix(objectname) & initialize data array ///
            if (rowexists)
            {
                cmddf.CommandSyntax = "dim(" + objectname + ")[1]";
                retres = analytics.ExecuteR(cmddf, true, false);
                if (retres != null)
                    dimrow = Int16.Parse(retres.ToString());
            }
            if (colexists)
            {
                cmddf.CommandSyntax = "dim(" + objectname + ")[2]";
                retres = analytics.ExecuteR(cmddf, true, false);
                if (retres != null)
                    dimcol = Int16.Parse(retres.ToString());
            }
            string[,] data = new string[dimrow, dimcol];
            //// now create FlexGrid and add to lst ///
            /////////finding Col headers /////
            cmddf.CommandSyntax = "colnames(" + objectname + ")";
            object colhdrobj = analytics.ExecuteR(cmddf, true, false);
            string[] colheaders;
            if (colhdrobj != null && !colhdrobj.ToString().Contains("Error"))
            {
                if (colhdrobj.GetType().IsArray)
                    colheaders = (string[])colhdrobj;//{ "Aa", "Bb", "Cc" };//
                else
                {
                    colheaders = new string[1];
                    colheaders[0] = colhdrobj.ToString();
                }
            }
            else
            {
                colheaders = new string[dimcol];
                for (int i = 0; i < dimcol; i++)
                    colheaders[i] = (i + 1).ToString();
            }

            /////////finding Row headers /////

            //read configuration and then decide to pull row headers

            string numrowheader = confService.AppSettings.Get("numericrowheaders");
            // load default value if no value is set 
            if (numrowheader.Trim().Length == 0)
                numrowheader = confService.DefaultSettings["numericrowheaders"];
            bool shownumrowheaders = numrowheader.ToLower().Equals("true") ? true : false; /// 

            cmddf.CommandSyntax = "rownames(" + objectname + ")";
            object rowhdrobj = analytics.ExecuteR(cmddf, true, false);
            string[] rowheaders;// = (string[])rowhdrobj;//{ "11", "22", "33" };
            if (rowhdrobj != null && !rowhdrobj.ToString().Contains("Error"))
            {
                if (rowhdrobj.GetType().IsArray)
                    rowheaders = (string[])rowhdrobj;//{ "Aa", "Bb", "Cc" };//
                else
                {
                    rowheaders = new string[1];
                    rowheaders[0] = rowhdrobj.ToString();
                }
            }
            else
            {
                rowheaders = new string[dimrow];
                //Type 1.//filling number for row header if rowheader is not present
                for (int i = 0; i < dimrow; i++)
                    rowheaders[i] = (i + 1).ToString();

            }

            bool isnumericrowheaders = true; // assuming that row headers are numeric
            short tnum;
            for (int i = 0; i < dimrow; i++)
            {
                if (!Int16.TryParse(rowheaders[i], out tnum))
                {
                    isnumericrowheaders = false; //row headers are non-numeric
                    break;
                }
            }

            if (isnumericrowheaders && !shownumrowheaders)
            {
                //Type 2.//filling empty values for row header if rowheader is not present
                for (int i = 0; i < dimrow; i++)
                    rowheaders[i] = "";
            }

            /// Populating array using data frame data
            
            bool isRowEmpty = true;//for Virtual. 
            int emptyRowCount = 0;//for Virtual. 
            List<int> emptyRowIndexes = new List<int>(); //for Virtual.
            string cellData = string.Empty;
            for (int r = 1; r <= dimrow; r++)
            {
                isRowEmpty = true;//for Virtual. 
                for (int c = 1; c <= dimcol; c++)
                {
                    if (dimcol == 1 && !dataclassname.ToLower().Equals("data.frame"))
                        cmddf.CommandSyntax = "as.character(" + objectname + "[" + r + "])";
                    else
                        cmddf.CommandSyntax = "as.character(" + objectname + "[" + r + "," + c + "])";

                    object v = analytics.ExecuteR(cmddf, true, false);
                    cellData = (v != null) ? v.ToString().Trim() : "";
                    data[r - 1, c - 1] = cellData;// v.ToString().Trim();

                    //for Virtual. // cell is non-empty in row, means row is non empty because of atleast 1 col
                    if (cellData.Length > 0)
                        isRowEmpty = false;
                }

                //for Virtual. // counting empty rows for virtual
                if (isRowEmpty)
                {
                    emptyRowCount++;
                    emptyRowIndexes.Add(r - 1);//making it zero based as in above nested 'for'
                }
            }

            // whether you want C1Flexgrid to be generated by using XML DOM or by Virtual class(Dynamic)
            bool DOMmethod = false;
            if (DOMmethod)
            {
                //12Aug2014 Old way of creating grid using DOM and then creating and filling grid step by step
                XmlDocument xdoc = createFlexGridXmlDoc(colheaders, rowheaders, data);
                //string xdoc = "<html><body><table><thead><tr><th class=\"h\"></th><th class=\"c\">A</th><th class=\"c\">B</th></tr></thead><tbody><tr><td class=\"h\">X</td><td class=\"c\">5</td><td class=\"c\">6</td></tr><tr><td class=\"h\">Y</td><td class=\"c\">8</td><td class=\"c\">9</td></tr></tbody></table></body></html>";
                createFlexGrid(xdoc, lst, headername);// headername = 'varname' else 'leftvarname' else 'objclass'
            }
            else//virutal list method
            {
                //There is no logic to remove empty rows in this vitual list method so
                //here we try to send data with non-empty rows by dropping empty ones first.
                if (emptyRowCount > 0)
                {
                    int nonemptyrowcount = dimrow - emptyRowCount;
                    string[,] nonemptyrowsdata = new string[nonemptyrowcount, dimcol];
                    string[] nonemptyrowheaders = new string[nonemptyrowcount];
                    for (int rr = 0, rrr = 0; rr < data.GetLength(0); rr++)
                    {
                        if (emptyRowIndexes.Contains(rr))//skip empty rows.
                            continue;
                        for (int cc = 0; cc < data.GetLength(1); cc++)
                        {
                            nonemptyrowsdata[rrr, cc] = data[rr, cc];//only copy non-empty rows
                        }
                        nonemptyrowheaders[rrr] = rowheaders[rr];//copying row headers too.
                        rrr++;
                    }
                    //Using Dynamic Class creation and then populating the grid. //12Aug2014
                    CreateDynamicClassFlexGrid(headername, colheaders, nonemptyrowheaders, nonemptyrowsdata, lst);
                }
                else
                {
                    //Using Dynamic Class creation and then populating the grid. //12Aug2014
                    CreateDynamicClassFlexGrid(headername, colheaders, rowheaders, data, lst);
                }
            }
            if (ow != null)//22May2014
                SendToOutput("", ref lst, ow);//send dataframe/matrix/array to output window or disk file
        }

        REngine engine;
        private void InitializeRDotNet()
        {
            REngine.SetEnvironmentVariables();
            engine = REngine.GetInstance();
            // REngine requires explicit initialization.
            // You can set some parameters.
            engine.Initialize();
            //load BSky and R packages
            engine.Evaluate("library(uadatapackage)");
            engine.Evaluate("library(foreign)");
            engine.Evaluate("library(data.table)");
            engine.Evaluate("library(RODBC)");
            engine.Evaluate("library(car)");
            engine.Evaluate("library(aplpack)");
            engine.Evaluate("library(mgcv)");
            engine.Evaluate("library(rgl)");
            engine.Evaluate("library(gmodels)");
        }
        private void RDotNetOpenDataset()
        {
            //open dataset
            engine.Evaluate("d2 <- UAloadDataset('D:/BlueSky/Projects/Xtras_Required/Data_Files/cars.sav',  filetype='SPSS', worksheetName=NULL, replace_ds=FALSE, csvHeader=TRUE, datasetName='Dataset1' )");
        }
        private void DisposeRDotNet()
        {
            engine.Dispose(); 
        }
        private void RDotNetExecute(OutputWindow ow)
        {
            CommandOutput lst = new CommandOutput();
            lst.IsFromSyntaxEditor = true;

            engine.Evaluate("BSky_One_Way_Anova = as.data.frame (summary(dd <- aov(mpg ~ year,data=Dataset1))[[1]])");
            engine.Evaluate("bskyfrmtobj <- BSkyFormat(BSky_One_Way_Anova)");
            CharacterMatrix cmatrix = engine.Evaluate("bskyfrmtobj").AsCharacterMatrix();
            string[,] mtx = new string[cmatrix.RowCount, cmatrix.ColumnCount];
            for (int r = 0; r < cmatrix.RowCount; r++)
            {
                for (int c = 0; c < cmatrix.ColumnCount; c++)
                {
                    mtx[r, c] = cmatrix[r, c];
                }
            }
            string objectname = "bskyfrmtobj";
            string headername = "This is generated in R.NET";

            CommandRequest cmddf = new CommandRequest();
            int dimrow = 1, dimcol = 1;
            bool rowexists = false, colexists = false;
            string dataclassname = string.Empty;

            //Find class of data passed. data.frame, matrix, or array
            cmddf.CommandSyntax = "class(" + objectname + ")"; // Row exists
            object retres = engine.Evaluate(cmddf.CommandSyntax).AsCharacter()[0];

            if (retres != null)
                dataclassname = retres.ToString();

            //find if dimension exists
            cmddf.CommandSyntax = "!is.na(dim(" + objectname + ")[1])"; // Row exists
            rowexists = engine.Evaluate(cmddf.CommandSyntax).AsLogical()[0];

            cmddf.CommandSyntax = "!is.na(dim(" + objectname + ")[2])";// Col exists
            colexists = engine.Evaluate(cmddf.CommandSyntax).AsLogical()[0];

            /// Find size of matrix(objectname) & initialize data array ///
            if (rowexists)
            {
                cmddf.CommandSyntax = "dim(" + objectname + ")[1]";
                retres = engine.Evaluate(cmddf.CommandSyntax).AsInteger()[0];
                if (retres != null)
                    dimrow = Int16.Parse(retres.ToString());
            }
            if (colexists)
            {
                cmddf.CommandSyntax = "dim(" + objectname + ")[2]";
                retres = engine.Evaluate(cmddf.CommandSyntax).AsInteger()[0];
                if (retres != null)
                    dimcol = Int16.Parse(retres.ToString());
            }
            string[,] data = new string[dimrow, dimcol];
            //// now create FlexGrid and add to lst ///
            /////////finding Col headers /////
            cmddf.CommandSyntax = "colnames(" + objectname + ")";
            CharacterVector colhdrobj = engine.Evaluate(cmddf.CommandSyntax).AsCharacter();
            string[] colheaders;
            if (colhdrobj != null && !colhdrobj.ToString().Contains("Error"))
            {
                if (true)//colhdrobj.GetType().IsArray)
                {
                    int siz = colhdrobj.Count();
                    colheaders = new string[siz];
                    for (int ri = 0; ri < siz; ri++)
                    {
                        colheaders[ri] = colhdrobj[ri];
                    }

                }
                else
                {
                    colheaders = new string[1];
                    colheaders[0] = colhdrobj.ToString();
                }
            }
            else
            {
                colheaders = new string[dimcol];
                for (int i = 0; i < dimcol; i++)
                    colheaders[i] = (i + 1).ToString();
            }

            /////////finding Row headers /////

            //read configuration and then decide to pull row headers

            bool shownumrowheaders = true; /// 

            cmddf.CommandSyntax = "rownames(" + objectname + ")";
            CharacterVector rowhdrobj =  engine.Evaluate(cmddf.CommandSyntax).AsCharacter();
            string[] rowheaders;// = (string[])rowhdrobj;//{ "11", "22", "33" };
            if (rowhdrobj != null && !rowhdrobj.ToString().Contains("Error"))
            {
                if (true)//rowhdrobj.GetType().IsArray)
                {
                    int siz = rowhdrobj.Count();
                    rowheaders = new string[siz];
                    for (int ri = 0; ri < siz; ri++)
                    {
                        rowheaders[ri] = rowhdrobj[ri];
                    }

                    //rowheaders = (string[])rowhdrobj;//{ "Aa", "Bb", "Cc" };//
                }
                else
                {
                    rowheaders = new string[1];
                    rowheaders[0] = rowhdrobj.ToString();
                }
            }
            else
            {
                rowheaders = new string[dimrow];
                //Type 1.//filling number for row header if rowheader is not present
                for (int i = 0; i < dimrow; i++)
                    rowheaders[i] = (i + 1).ToString();

            }

            bool isnumericrowheaders = true; // assuming that row headers are numeric
            short tnum;
            for (int i = 0; i < dimrow; i++)
            {
                if (!Int16.TryParse(rowheaders[i], out tnum))
                {
                    isnumericrowheaders = false; //row headers are non-numeric
                    break;
                }
                //if (i == 10)//just cheking first 10 numbers for being int. Not cheking all the row headers.
                //    break;
            }

            if (isnumericrowheaders && !shownumrowheaders)
            {
                //Type 2.//filling empty values for row header if rowheader is not present
                for (int i = 0; i < dimrow; i++)
                    rowheaders[i] = "";
            }

            /// Populating array using data frame data
            bool isRowEmpty = true;//for Virtual. 
            int emptyRowCount = 0;//for Virtual. 
            List<int> emptyRowIndexes = new List<int>(); //for Virtual.
            string cellData = string.Empty;
            for (int r = 1; r <= dimrow; r++)
            {
                isRowEmpty = true;//for Virtual. 
                for (int c = 1; c <= dimcol; c++)
                {
                    if (dimcol == 1 && !dataclassname.ToLower().Equals("data.frame"))
                        cmddf.CommandSyntax = "as.character(" + objectname + "[" + r + "])";
                    else
                        cmddf.CommandSyntax = "as.character(" + objectname + "[" + r + "," + c + "])";

                    object v = engine.Evaluate(cmddf.CommandSyntax).AsCharacter()[0];
                    cellData = (v != null) ? v.ToString().Trim() : "";
                    data[r - 1, c - 1] = cellData;// v.ToString().Trim();

                    //for Virtual. // cell is non-empty in row, means row is non empty because of atleast 1 col
                    if (cellData.Length > 0)
                        isRowEmpty = false;
                }

                //for Virtual. // counting empty rows for virtual
                if (isRowEmpty)
                {
                    emptyRowCount++;
                    emptyRowIndexes.Add(r - 1);//making it zero based as in above nested 'for'
                }
            }

            // whether you want C1Flexgrid to be generated by using XML DOM or by Virtual class(Dynamic)
            bool DOMmethod = false;
            if (DOMmethod)
            {
                //12Aug2014 Old way of creating grid using DOM and then creating and filling grid step by step
                XmlDocument xdoc = createFlexGridXmlDoc(colheaders, rowheaders, data);
                //string xdoc = "<html><body><table><thead><tr><th class=\"h\"></th><th class=\"c\">A</th><th class=\"c\">B</th></tr></thead><tbody><tr><td class=\"h\">X</td><td class=\"c\">5</td><td class=\"c\">6</td></tr><tr><td class=\"h\">Y</td><td class=\"c\">8</td><td class=\"c\">9</td></tr></tbody></table></body></html>";
                createFlexGrid(xdoc, lst, headername);// headername = 'varname' else 'leftvarname' else 'objclass'
            }
            else//virutal list method
            {
                //There is no logic to remove empty rows in this vitual list method so
                //here we try to send data with non-empty rows by dropping empty ones first.
                if (emptyRowCount > 0)
                {
                    int nonemptyrowcount = dimrow - emptyRowCount;
                    string[,] nonemptyrowsdata = new string[nonemptyrowcount, dimcol];
                    string[] nonemptyrowheaders = new string[nonemptyrowcount];
                    for (int rr = 0, rrr = 0; rr < data.GetLength(0); rr++)
                    {
                        if (emptyRowIndexes.Contains(rr))//skip empty rows.
                            continue;
                        for (int cc = 0; cc < data.GetLength(1); cc++)
                        {
                            nonemptyrowsdata[rrr, cc] = data[rr, cc];//only copy non-empty rows
                        }
                        nonemptyrowheaders[rrr] = rowheaders[rr];//copying row headers too.
                        rrr++;
                    }
                    //Using Dynamic Class creation and then populating the grid. //12Aug2014
                    CreateDynamicClassFlexGrid(headername, colheaders, nonemptyrowheaders, nonemptyrowsdata, lst);
                }
                else
                {
                    //Using Dynamic Class creation and then populating the grid. //12Aug2014
                    CreateDynamicClassFlexGrid(headername, colheaders, rowheaders, data, lst);
                }
            }
            if (ow != null)//22May2014
                SendToOutput("", ref lst, ow);//send dataframe/matrix/array to output window or disk file
        }


        //No Need of following. Logic changed
        //15Apr2014. List location 1 will have listname and number of tables it caontains. Location 2 onwards are tables. 1 table per location.
        private void FormatBSkyList2(CommandOutput lst, string objectname, string headername, OutputWindow ow) // for BSky list2 processing
        {
            MessageBox.Show(this, "BSkyList2 Processing... close this box");
        }

        private void SendErrorToOutput(string ewmessage, OutputWindow ow) //03Jul2013 error warning message
        {
            object o;
            CommandRequest cmd = new CommandRequest();
            string sinkfilefullpathname = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("tempsink"));//23nov2012
            // load default value if no path is set or invalid path is set
            if (sinkfilefullpathname.Trim().Length == 0 || !IsValidFullPathFilename(sinkfilefullpathname, false))
            {
                MessageBox.Show(this, "Key 'tempsink' not found in config file. Aborting...");
                return;
            }
            OpenSinkFile(@sinkfilefullpathname, "wt"); //06sep2012
            SetSink(); //06sep2012

            cmd.CommandSyntax = "write(\"" + ewmessage + ".\",fp)";// command 
            o = analytics.ExecuteR(cmd, false, false);

            ResetSink();//06sep2012
            CloseSinkFile();//06sep2012
            CreateOuput(ow);//06sep2012
        }

        //18Dec2013 Sending command to output to make them appear different than command-output
        private void SendCommandToOutput(string command, string NameOfAnalysis)
        {
            string rcommcol = confService.GetConfigValueForKey("rcommcol");//23nov2012
            byte red = byte.Parse(rcommcol.Substring(3, 2), NumberStyles.HexNumber);
            byte green = byte.Parse(rcommcol.Substring(5, 2), NumberStyles.HexNumber);
            byte blue = byte.Parse(rcommcol.Substring(7, 2), NumberStyles.HexNumber);
            Color c = Color.FromArgb(255, red, green, blue);

            CommandOutput lst = new CommandOutput();
            lst.NameOfAnalysis = NameOfAnalysis; // left tree Parent
            AUParagraph Title = new AUParagraph();
            Title.Text = command; //right side contents
            Title.FontSize = BSkyStyler.BSkyConstants.TEXT_FONTSIZE;//10Nov2014 //12; //right side content size
            Title.textcolor = new SolidColorBrush(c);//new SolidColorBrush(Colors.DeepSkyBlue);
            Title.ControlType = "Command"; // left tree child
            lst.Add(Title);

            AddToSession(lst);
        }

        private void SplitBSkyFormat(string command, out string sub, out string varname, out string BSkyLeftVar)
        {
            /// patterns is like: "BSkyFormat(var.name <- func.name("
            string pattern = @"BSkyFormat\(\s*[A-Za-z0-9_\.]+\s*(<-|=)\s*[A-Za-z0-9_\.]+\(";
            int firstindex = command.IndexOf('(');
            int lastindex = command.LastIndexOf(')');
            sub = "";
            varname = "";
            BSkyLeftVar = "";

            if (firstindex == -1 || lastindex == -1)//21May2014 This check is important to stop app crash.
            {
                return;
            }

            #region Finding what's passed in 'BSkyFormat(param)' as parameter.
            sub = command.Substring(firstindex + 1, lastindex - firstindex - 1);
            /// sub can be one of :
            /// sub = df
            /// sub = df <- data.frame(...)
            /// sub = data.frame(...)
            #endregion

            #region Finding Leftvar in Leftvar <- BSkyFormat
            ///See if BSkyFormat is assigned to any leftvar. eg.. leftvar <- BSkyFormat(....)
            int assignmentIndex, BSkyIndex;
            BSkyLeftVar = "";
            BSkyIndex = command.Trim().IndexOf("BSkyFormat");
            assignmentIndex = command.Trim().LastIndexOf("<-", BSkyIndex);
            if (assignmentIndex < 0)//arrow is not there
            {
                assignmentIndex = command.Trim().LastIndexOf("=", BSkyIndex);
            }
            if (assignmentIndex > 0)//<- or = before BSkyFormat found. ie. leftvar <- BskyFromat
            {
                ///find 'leftvar' name now
                BSkyLeftVar = command.Trim().Substring(0, assignmentIndex).Trim();
            }
            #endregion

            #region Finding varname in BSkyFormat(varname <- data.frame(...) )
            /// looking for parameter of type df <- data.frame(...)
            bool str = Regex.IsMatch(command, pattern);
            MatchCollection mc = Regex.Matches(command, pattern);


            int asnmntindex = 0;
            if (str)
            {
                asnmntindex = sub.IndexOf("<-");
                if (asnmntindex < 0)
                {
                    asnmntindex = sub.IndexOf('=');
                }
                varname = sub.Substring(0, asnmntindex);
            }
            else // for BSkyFormat(m) then varname = m 
            {
                pattern = @"\s*\G[A-Za-z0-9_\.]+\s*[^\(\)\:,;=]?";
                str = Regex.IsMatch(sub, pattern);
                mc = Regex.Matches(sub, pattern);
                if (str && mc.Count == 1 && (sub.Trim().Length == mc[0].ToString().Trim().Length))
                {
                    varname = mc[0].ToString();
                }
            }
            #endregion
            varname = varname.Trim();//01Jul2013
            return;

        }

        //23Sep2014 
        //splitting BSkyFormat parameters in two parts. 
        //First part is object to be formatted(must be first param in call)
        //Second part has all the remaining parameters
        private void SplitBSkyFormatParams(string command, out string first, out string rest, out string usertitle)
        {
            string firstParam = string.Empty;//for object to be formatted
            string restParams = string.Empty;//for remaining params
            usertitle = string.Empty; //for title if passed in function call by the user.

            //find header/title
            int ttlidx = command.IndexOf("singleTableOutputHeader");
            int eqlidx = 0;
            int commaidx = 0;
            int closebracketidx = 0;
            int headerstartidx = 0;
            int headerendidx = 0;//modify this to right value
            if (ttlidx > 0) //if title is provided
            {
                eqlidx = command.IndexOf("=", ttlidx);
                if (eqlidx > ttlidx)
                {
                    headerstartidx = eqlidx + 1;
                    headerendidx = eqlidx + 1;//modify this to right value
                    commaidx = command.IndexOf(",", eqlidx);
                    closebracketidx = command.IndexOf(")", eqlidx);
                    //Now closing bracket will always be present but comma may or may not be. 
                    //Depending on if singleTableOutputHeader is last param or not.
                    if (commaidx > eqlidx) //comma is present
                    {
                        headerendidx = commaidx - 1;
                    }
                    else //comma not present so end marker is closing bracket
                    {
                        headerendidx = closebracketidx - 1;
                    }
                }
                usertitle = command.Substring(headerstartidx, (headerendidx - headerstartidx) + 1);
            }


            ///Logic Starts
            //look for first comma and split the parameters in two
            int paramstart = command.IndexOf("BSkyFormat(") + 11;//11 is the length of BSkyFormat(
            string allParams = command.Substring(paramstart, command.Length - 12); //ignore last ')'
            int indexOfComma = -1;
            int brackets = 0;
            for (int idx = 0; idx < allParams.Length; idx++)
            {
                if (allParams.ElementAt(idx) == '(')
                    brackets++;
                else if (allParams.ElementAt(idx) == ')')
                    brackets--;
                else if (brackets == 0 && allParams.ElementAt(idx) == ',')
                    indexOfComma = idx;
                else
                    continue;

                if (brackets == 0 && indexOfComma > 0)
                    break;
            }
            ///Logic Ends
            if (indexOfComma < 0) // comma not found, means no other params are in this func call.
            {
                first = allParams;
                rest = string.Empty;
            }
            else
            {
                first = allParams.Substring(0, indexOfComma);
                rest = allParams.Substring(indexOfComma + 1);
            }
        }

        private void ExecuteOtherCommand(OutputWindow ow, string stmt)
        {

            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Before Executing in R.", LogLevelEnum.Info);


            #region Check open close brackets before executing
            string unbalmsg;
            if (!AreBracketsBalanced(stmt, out unbalmsg))//if unbalanced brackets
            {
                CommandRequest errmsg = new CommandRequest();
                string fullmsg = "Error : " + unbalmsg;
                errmsg.CommandSyntax = "write(\"" + fullmsg.Replace("<", "&lt;").Replace('"', '\'') + "\",fp)";//
                analytics.ExecuteR(errmsg, false, false); //for printing command in file
                return;
            }
            #endregion
            ///if command is for loading dataset //
            if (stmt.Contains("UAloadDataset"))
            {
                int indexofopening = stmt.IndexOf('(');
                int indexofclosing = stmt.IndexOf(')');
                string[] parameters = stmt.Substring(indexofopening + 1, indexofclosing - indexofopening - 2).Split(',');
                string filename = string.Empty;
                foreach (string s in parameters)
                {
                    if (s.Contains('/') || s.Contains('\\'))
                        filename = s.Replace('\"', ' ').Replace('\'', ' ');
                }
                if (filename.Contains("="))
                {
                    filename = filename.Substring(filename.IndexOf("=") + 1);
                }
                FileOpenCommand fo = new FileOpenCommand();
                fo.FileOpen(filename.Trim());
                return;
            }

            object o = null;
            CommandRequest cmd = new CommandRequest();

            if (stmt.Contains("BSkySortDataframe(") ||
                stmt.Contains("BSkyComputeExpression(") || stmt.Contains("BSkyRecode("))
            {
                CommandExecutionHelper auacb = new CommandExecutionHelper();
                UAMenuCommand uamc = new UAMenuCommand();
                uamc.bskycommand = stmt;
                uamc.commandtype = stmt;
                auacb.ExeuteSingleCommandWithtoutXML(stmt);//auacb.ExecuteSynEdtrNonAnalysis(uamc);
                auacb.Refresh_Statusbar();
                //auacb = null;
            }
            else
            {

                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Executing in R.", LogLevelEnum.Info);

 
                 isVariable(ref stmt);///for checking if its variable then it must be enclosed within print();
                                      
                //27May2015 check if command is graphic and get its height width and then reopen graphic device with new dimensions
                 if (lastcommandwasgraphic)//listed graphic
                 {
                     if (imgDim!=null && imgDim.overrideImgDim)
                     {
                         CloseGraphicsDevice();
                         OpenGraphicsDevice(imgDim.imagewidth, imgDim.imageheight); // get image dimenstion from external source for this particular graphic.
                     }
                 }


                cmd.CommandSyntax = stmt;// command 
                o = analytics.ExecuteR(cmd, false, false);   //// get Direct Result and write in sink file

                CommandRequest cmdprn = new CommandRequest();
                if (o != null && o.ToString().Contains("Error"))//for writing some of the errors those are not taken care by sink.
                {
                    cmdprn.CommandSyntax = "write(\"" + o.ToString() + "\",fp)";// http://www.w3schools.com/xml/xml_syntax.asp
                    o = analytics.ExecuteR(cmdprn, false, false); /// for printing command in file
                    
                    ///if there is error in assignment, like RHS caused error and LHS var is never updated
                    ///Better make LHS var null.
                    string lhvar = string.Empty;
                    GetLeftHandVar(stmt, out lhvar);
                    if (lhvar != null)
                    {
                        cmd.CommandSyntax = lhvar+" <- NULL";// assign null 
                        o = analytics.ExecuteR(cmd, false, false);
                    }
                }
            }

            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: After Executing in R.", LogLevelEnum.Info);

            
        }

        private void GetLeftHandVar(string stmt, out string lhvar)
        {
            lhvar = null;//null if no left hand var exists in stmt
            int eqidx = stmt.IndexOf("=");
            int arrowidx = stmt.IndexOf("<-");
            int lowestidx = 0;
            bool isvarname = true; // assuming lefthand substring is variable.
            if (eqidx < 0 && arrowidx < 0)//no "=" and no "<-"
                return;

            //if one of the index is -1 then better overwrite that with value of the other
            if (eqidx == -1) eqidx = arrowidx;
            else if (arrowidx == -1) arrowidx = eqidx;

            //find the lowest(leftmost) index between = and <-
            if (eqidx < arrowidx)
            {
                lowestidx = eqidx;// index of =
            }
            else
            {
                lowestidx = arrowidx;//index of <-
            }

            string subs = stmt.Substring(0, lowestidx).Trim();

            //you can add more invalid chars to following 'if'
            if (subs.Contains(" ") || subs.Contains("(") || subs.Contains(")") ||
                subs.Contains("{") || subs.Contains("}")
                )
            {
                isvarname = false;
            }
            if (isvarname)//if subs is variable
            {
                lhvar = subs;
            }
        }

        ///Finding if R command is a method call that can return some results.
        private bool IsMethodName(string stmt)//01May2013
        {
            string pattern = @"\s*[A-Za-z0-9_\.]+\s*\(\s*";// method name patthern, methodName(
            bool str = Regex.IsMatch(stmt, pattern);
            MatchCollection mc = Regex.Matches(stmt.Trim(), pattern);
            foreach (Match m in mc)
            {
                if (m.Index == 0)
                    return true;
            }
            return false;
        }

        //For Painting Output window for BSKyFormated object. And/Or to refresh datagrid for non-analytics commands like sort, compute
        private void RefreshOutputORDataset(string objectname, CommandRequest cmd, string originalCommand, OutputWindow ow)
        {
            UAMenuCommand uamc = new UAMenuCommand();
            cmd.CommandSyntax = "is.null(" + objectname + "$BSkySplit)";//$BSkySplit or $split in return structure
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Executing : " + cmd.CommandSyntax, LogLevelEnum.Info);
            // is it just a list(non Bsky) or its a list that contains tables (user tables or Bsky Stat result tables)
            bool isNonBSkyList = false;
            object isNonBSkyListstr = analytics.ExecuteR(cmd, true, false);
            if (isNonBSkyListstr != null && isNonBSkyListstr.ToString().ToLower().Equals("true"))
            {
                isNonBSkyList = true;
            }
            if (isNonBSkyList)
            {
                string ewmessage = "This Object cannot be formatted using BSKyFormat. BSkyFormat can be used on Array, Matrix, Data Frame and BSky List objects only.";
                SendErrorToOutput(ewmessage, ow);//03Jul2013
                return;
            }
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: $BSkySplit Result (false means non-bsky list): " + isNonBSkyList, LogLevelEnum.Info);

            cmd.CommandSyntax = objectname + "$uasummary[[7]]";
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Executing : " + cmd.CommandSyntax, LogLevelEnum.Info);

            string bskyfunctioncall = (string)analytics.ExecuteR(cmd, true, false);//actual call with values
            if (bskyfunctioncall == null)
            {
                bskyfunctioncall = ""; //24Apr2014 This is when no Dataset is open. And Syntax editor is open.Not returning, instead putting blank.
            }
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: $uasummary[[7]] Result : " + bskyfunctioncall, LogLevelEnum.Info);
            string bskyfunctionname = "";
            if (bskyfunctioncall.Length > 0)
            {
                if (bskyfunctioncall.Contains("("))
                    bskyfunctionname = bskyfunctioncall.Substring(0, bskyfunctioncall.IndexOf('(')).Trim();
                else
                    bskyfunctionname = bskyfunctioncall;
            }
            uamc.commandformat = objectname;// object that stores the result of analysis
            uamc.bskycommand = bskyfunctioncall.Replace('\"', '\'');// actual BSkyFunction call. " quotes replaced by '
            uamc.commandoutputformat = bskyfunctionname.Length > 0 ? string.Format(@"{0}", BSkyAppData.BSkyDataDirConfigBkSlash) + bskyfunctionname + ".xml" : "";//23Apr2015 
            uamc.commandtemplate = bskyfunctionname.Length > 0 ? string.Format(@"{0}", BSkyAppData.BSkyDataDirConfigBkSlash) + bskyfunctionname + ".xaml" : "";//23Apr2015 

            uamc.commandtype = originalCommand;
            CommandExecutionHelper auacb = new CommandExecutionHelper();
            if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: CommandExecutionHelper: " , LogLevelEnum.Info);
            auacb.ExecuteSyntaxEditor(uamc, saveoutput.IsChecked == true ? true : false);//10JAn2013 edit
            auacb = null;

        }

        //was private
        public void RefreshDatagrids()//16May2013
        {
            string stmt = "Refresh Grids";
            UAMenuCommand uamc = new UAMenuCommand();
            uamc.commandformat = stmt;
            uamc.bskycommand = stmt;
            uamc.commandtype = stmt;
            CommandExecutionHelper auacb = new CommandExecutionHelper();
            auacb.DatasetRefreshAndPrintTitle("Refresh Data");
        }

        //16Jul2015 refesh both grids when 'refresh' icon is clicked in output window
        public void RefreshBothgrids()//16Jul2015
        {
            string stmt = "Refresh Grids";
            UAMenuCommand uamc = new UAMenuCommand();
            uamc.commandformat = stmt;
            uamc.bskycommand = stmt;
            uamc.commandtype = stmt;
            CommandExecutionHelper auacb = new CommandExecutionHelper();
            auacb.BothGridRefreshAndPrintTitle("Refresh Data");
        }


        private void isVariable(ref string stmt) // checks if user want to print a variable. Or function call results.
        {
            int frstopenbrkt = -2, secopenbrkt = -2, closingbrkt = -2, arowassignidx = -2, eqassignidx, assignidx = -2;
            bool beginswithprint = stmt.StartsWith("print(") || stmt.StartsWith("(");
            if (beginswithprint)
            {
                frstopenbrkt = stmt.IndexOf("(");
                secopenbrkt = stmt.IndexOf("(", frstopenbrkt + 1);

            }
            else
            {
                secopenbrkt = stmt.IndexOf("(");
            }

            closingbrkt = stmt.IndexOf(")", secopenbrkt + 1);

            bool hasassignmentoperator = (stmt.Contains("=") || stmt.Contains("<-"));
            if (hasassignmentoperator && secopenbrkt > 0)
            {
                eqassignidx = stmt.IndexOf("=");
                arowassignidx = stmt.IndexOf("<-");
                if (eqassignidx > 0 && arowassignidx > 0) // both assignment oprator exists. say: a<-func(q=TRUE)
                {
                    assignidx = eqassignidx < arowassignidx ? eqassignidx : arowassignidx; // whichever has less idx
                }
                else if (eqassignidx > 0) // only '=' exists
                {
                    assignidx = eqassignidx;
                }
                else if (arowassignidx > 0)//only "<-" exists
                {
                    assignidx = arowassignidx;
                }

                //its not assignment of type 'aa = something'. Rather its assignment in parameters like 'somfunc(isgood=TRUE)'
                if (assignidx > secopenbrkt && closingbrkt > assignidx)//if closing to secopenbrkt closes after assignment'='
                    hasassignmentoperator = false;
            }

            bool beginswithcat = stmt.StartsWith("cat(");

            if (beginswithprint && hasassignmentoperator) // for print(a<-c('msg')
            {
                int indexofopeningprintparenthesis = stmt.IndexOf('(');// first (
                int indexofclosingprintparenthesis = stmt.LastIndexOf(')');// last )
                int indexofassignmentopr = stmt.IndexOf("<-") > 0 ? stmt.IndexOf("<-") : stmt.IndexOf('=');
                string varname = stmt.Substring(indexofopeningprintparenthesis + 1, (indexofassignmentopr - indexofopeningprintparenthesis - 1));// a
                stmt = stmt.Remove(indexofclosingprintparenthesis).Replace("print(", "") + "; print(" + varname + ");";// a<-c('msg'); print(a);
            }
            if (!beginswithprint && !hasassignmentoperator && !beginswithcat)// for a & a+ a+a+a*2 & help(...) & ?...
            {
                stmt = "print(" + stmt + ")";
            }


        }

        // Check if command is one of : if, for, while, function
        private bool isConditionalOrLoopingCommand(string extractedCommand)//07Sep2012
        {
            bool iscondloop = false;

            if (extractedCommand.Equals("function(") || extractedCommand.Equals("for(") ||
                extractedCommand.Equals("while(") || extractedCommand.Equals("if(") ||
                extractedCommand.Equals("{"))
            {
                iscondloop = true;
            }
            return iscondloop;
        }

        // Creating global list of graphic command in begining //
        private void LoadRegisteredGraphicsCommands(string grpListPath)//07Sep2012
        {
            //string grpListPath = @".\Config\GraphicCommandList.txt";
            registeredGraphicsList.Clear();//clearing just as precaution. Not actually needed
            string line;
            string[] lineparts;

            char[] separators = new char[] { ',' };//{',', ' ', ';'};
            string keyGraphicCommand;
            int grphwidth=-1, grphheight=-1; // -1 means use defauls from the options setting.
            StreamReader f = null;
            try
            {
                f = new StreamReader(grpListPath);
                while ((line = f.ReadLine()) != null)
                {
                    grphwidth=-1; grphheight=-1;
                    if (line.Trim().Length > 0)//do not add blank lines
                    {
                        if (line.Trim().StartsWith("#"))//commented line in GraphicCommandList.txt. Only single line comment is supported.
                            continue;
                        lineparts = (line.Trim()).Split(separators);
                        keyGraphicCommand = lineparts[0].Trim();
                        if(lineparts.Length>1 && lineparts[1]!=null && lineparts[1].Trim().Length>0 )//get width from file(line)
                        {
                            Int32.TryParse(lineparts[1], out grphwidth);//try setting width from file.
                        }
                        if (lineparts.Length > 2 && lineparts[2] != null && lineparts[2].Trim().Length > 0)//get height from file(line)
                        {
                            if(!Int32.TryParse(lineparts[2], out grphheight))//if conversion fails
                            {
                                if(grphwidth>0)
                                    grphheight = grphwidth; // make height same as width, if height is not provided
                            }
                        }
                        
                        //now add three values to dictionary
                        if(!registeredGraphicsList.ContainsKey(keyGraphicCommand)) // if the key is not already present. Unique keys are entertained.
                            registeredGraphicsList.Add(keyGraphicCommand, new ImageDimensions(){ imagewidth = grphwidth, imageheight = grphheight} );
                    }
                }
                f.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(this, "Error opening registered graphic command list.\n" + e.Message);
                logService.WriteToLogLevel("Registered Graphics List not found!", LogLevelEnum.Error);
            }
        }

        ImageDimensions imgDim; //store image dimentions of current graphic, from graphiccommand.txt
        bool lastcommandwasgraphic = false;
        // Check if graphic command belongs to global list of graphic command //
        private bool isGraphicCommand(string extractedCommand)//07Sep2012
        {
            string command = extractedCommand.Replace('(', ' ').Trim(); //remove the open parenthesis from the end.
            //// searching list ////
            bool graphic = false;
            string tempcomm;

            if (registeredGraphicsList.ContainsKey(command))
            {
                registeredGraphicsList.TryGetValue(command, out imgDim);
                graphic = true;
            }
            lastcommandwasgraphic = graphic;
            return graphic;
        }

        //if command is single command(or may be 2 lines one for BSkyFormat) then find if XML template is defined
        private bool isXMLDefined()
        {
            bool hasXML = false;
            if (DlgProp != null)
                hasXML = DlgProp.IsXMLDefined;
            return hasXML;
        }

        // Extracts command name in format like :- "any.command("
        private string ExtractCommandName(string stmt)
        {
            string comm = string.Empty;
            stmt = RemoveExtraSpacesBeforeOpeningBracket(stmt);

            string pattern = @"[A-Za-z0-9_.]+\(";
            bool com = Regex.IsMatch(stmt, pattern);

            if (com)//remove extra spaces
            {
                MatchCollection mc = Regex.Matches(stmt, pattern);
                foreach (Match m in mc)
                {
                    comm = m.Value;//picking up the very first command only. Which should be one of
                    break;          // if(, for(, while(, function( or plot( etc..
                }
            }
            else//28Aug2014 if no command name was found return the same string that was received as a parameter
            {
                comm = stmt;
            }
            return comm.Trim();
        }

        // Removes extra spaces between "any.command" and "(" if command is "any.command    ("
        private string RemoveExtraSpacesBeforeOpeningBracket(string stmt)
        {
            string spacelessstmt = stmt;
            string pattern = @"[A-Za-z0-9_.]+\s+\(";
            bool spaces = Regex.IsMatch(stmt, pattern);

            if (spaces)//remove extra spaces
            {
                MatchCollection mc = Regex.Matches(stmt, pattern);
                spacelessstmt = Regex.Replace(stmt, @"\s+\(", "(", RegexOptions.None);
            }
            return spacelessstmt;
        }

        private void createAUPara(string auparas, CommandOutput lst)
        {
            string startdatetime = string.Empty;
            if (lst.NameOfAnalysis == null || lst.NameOfAnalysis.Trim().Length < 1)
            {
                lst.NameOfAnalysis = "R-Output";//Parent Node name. 02Aug2012
            }
            if (auparas == null || auparas.Trim().Length < 1)
                return;
            string selectnode = "bskyoutput/bskyanalysis/aup";
            string AUPara = "<aup>" + auparas.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</aup>";//single AUPara for all lines
            XmlDocument xd = null;

            ///// Creating DOM for generation output ///////
            string fulldom = "<bskyoutput> <bskyanalysis>" + AUPara + "</bskyanalysis> </bskyoutput>";
            xd = new XmlDocument(); xd.LoadXml(fulldom);

            //// for creating AUPara //////////////
            BSkyOutputGenerator bsog = new BSkyOutputGenerator();
            int noofaup = xd.SelectNodes(selectnode).Count;// should be 3

            for (int k = 1; k <= noofaup; k++)
            {
                if (lst.NameOfAnalysis.Equals("R-Output") || lst.NameOfAnalysis.Contains("Command Editor Execution"))
                {
                    lst.Add(bsog.createAUPara(xd, selectnode + "[" + (1) + "]", ""));
                }
                else if (lst.NameOfAnalysis.Equals("Datasets"))
                {
                    lst.Add(bsog.createAUPara(xd, selectnode + "[" + (1) + "]", "Open Datasets"));
                }
                else
                {
                    lst.Add(bsog.createAUPara(xd, selectnode + "[" + k + "]", startdatetime));
                }
            }
        }

        private void createBSkyGraphic(Image img, CommandOutput lst)//30Aug2012
        {
            lst.NameOfAnalysis = "R-Graphics";
            BSkyGraphicControl bsgc = new BSkyGraphicControl();
            bsgc.BSkyImageSource = img.Source;
            bsgc.ControlType = "Graphic";
            lst.Add(bsgc);
        }

        private void createDiskFileFromImageSource(BSkyGraphicControl bsgc)
        {
            Image myImage = new System.Windows.Controls.Image();
            myImage.Source = bsgc.BSkyImageSource;
            string grpctrlimg = Path.Combine(System.IO.Path.GetTempPath().Replace("\\", "/"), confService.GetConfigValueForKey("bskygrphcntrlimage"));//23nov2012
            // load default value if no path is set or invalid path is set
            if (grpctrlimg.Trim().Length == 0 || !IsValidFullPathFilename(grpctrlimg, false))
            {
                MessageBox.Show(this, "Key 'bskygrphcntrlimage' not found in config file. Aborting...");
                return;
            }

            System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            bitmapImage = ((System.Windows.Media.Imaging.BitmapImage)myImage.Source);
            System.Windows.Media.Imaging.PngBitmapEncoder pngBitmapEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            System.IO.FileStream stream = new System.IO.FileStream(@grpctrlimg, FileMode.Create);

            pngBitmapEncoder.Interlace = PngInterlaceOption.On;
            pngBitmapEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage));
            pngBitmapEncoder.Save(stream);
            stream.Flush();
            stream.Close();
        }

        private XmlDocument createFlexGridXmlDoc(string[] colheaders, string[] rowheaders, string[,] data)
        {
            XmlDocument xd = new XmlDocument();
            int rc = rowheaders.Length;
            int cc = colheaders.Length;
            string colnames = "<table  cellpadding=\"5\" cellspacing=\"0\"><thead><tr><th class=\"h\"></th>";
            foreach (string s in colheaders)
            {
                //11Jul2014added to avoid crash when colnames are not there. There must be something.
                if (s == null || s.Trim().Length < 1)
                    colnames = colnames + "<th class=\"c\">" + ".-." + "</th>";//".-.'" is spl char sequence that tells that there is no header
                else
                    colnames = colnames + "<th class=\"c\">" + s + "</th>";
            }
            colnames = colnames + "</tr></thead>";

            //// creating row headers with data ie.. one complete row ////
            string rowdata = "<tbody>";
            for (int r = 0; r < rc; r++)
            {
                //Putting Row Header in a row. ".-.'" is spl char sequence that tells that there is no header
                if (bsky_no_row_header)
                {
                    rowdata = rowdata + "<tr><td class=\"h\">" + ".-." + "</td>";//rowheader in row
                }
                else
                {
                    rowdata = rowdata + "<tr><td class=\"h\">" + rowheaders[r] + "</td>";//rowheader in row
                }
                //Putting Data in a row
                for (int c = 0; c < cc; c++)/// data in row
                {
                    rowdata = rowdata + "<td class=\"c\">" + data[r, c].Replace("<", "&lt;").Replace(">", "&gt;") + "</td>";
                }
                rowdata = rowdata + "</tr>";
            }
            rowdata = rowdata + "</tbody></table>";
            string fullxml = colnames + rowdata;
            xd.LoadXml(fullxml);
            return xd;
        }

        private void createFlexGrid(XmlDocument xd, CommandOutput lst, string header)
        {
            AUXGrid xgrid = new AUXGrid();
            AUGrid c1FlexGrid1 = xgrid.Grid;// new C1flexgrid.
            xgrid.Header.Text = header;//FlexGrid header as well as Tree leaf node text(ControlType)
            //XmlDocument xd = new XmlDocument(); xd.LoadXml(xdoc);

            BSkyOutputGenerator bsog = new BSkyOutputGenerator();
            bsog.html2flex(xd, c1FlexGrid1);
            lst.Add(xgrid);
        }

        #region Dynamic Class creation and filling C1Flexgrid

        private void CreateDynamicClassFlexGrid(string header, string[] colheaders, string[] rowheaders, string[,] data, CommandOutput lst)
        {
            IList list;
            AUXGrid xgrid = new AUXGrid();
            AUGrid c1FlexGrid1 = xgrid.Grid;// new C1flexgrid.
            xgrid.Header.Text = header;//FlexGrid header as well as Tree leaf node text(ControlType)

            ///////////// merge and sizing /////
            c1FlexGrid1.AllowMerging = AllowMerging.ColumnHeaders | AllowMerging.RowHeaders;
            c1FlexGrid1.AllowSorting = false;

            //trying to fix the size of the grid so that rendering does not take much time calculating these
            c1FlexGrid1.MaxHeight = 800;// NoOfRows* EachRowHeight;
            c1FlexGrid1.MaxWidth = 1000;

            int nrows = data.GetLength(0);
            int ncols = data.GetLength(1);

            //// Dynamic class logic
            FGDataSource ds = new FGDataSource();
            ds.RowCount = nrows;
            ds.Data = data;
            foreach (string s in colheaders)
            {
                ds.Variables.Add(s.Trim());
            }
            list = new DynamicList(ds);
            if (list != null)
            {
                c1FlexGrid1.ItemsSource = list;
            }
            FillColHeaders(colheaders, c1FlexGrid1);
            FillRowHeaders(rowheaders, c1FlexGrid1);
            lst.Add(xgrid);
        }

        private void FillColHeaders(string[] colHeaders, AUGrid c1fgrid)
        {
            bool iscolheaderchecked = true;// rowheaderscheck.IsChecked == true ? true : false;
            //creating row headers
            string[,] colheadersdata = new string[1, colHeaders.Length];
            ////creating data
            for (int r = 0; r < colheadersdata.GetLength(0); r++)
            {
                for (int c = 0; c < colheadersdata.GetLength(1); c++)
                {
                    colheadersdata[r, c] = colHeaders[c];
                }
            }

            //create & fill row headers
            bool fillcolheaders = iscolheaderchecked;
            if (fillcolheaders)
            {
                var FGcolheaders = c1fgrid.ColumnHeaders; 
                FGcolheaders.Rows[0].AllowMerging = true;
                FGcolheaders.Rows[0].HorizontalAlignment = HorizontalAlignment.Center;

                for (int i = FGcolheaders.Columns.Count; i < colheadersdata.GetLength(1); i++)
                {
                    C1.WPF.FlexGrid.Column col = new C1.WPF.FlexGrid.Column();
                    col.AllowMerging = true;
                    col.VerticalAlignment = VerticalAlignment.Top;
                    FGcolheaders.Columns.Add(col);
                }

                for (int i = FGcolheaders.Rows.Count; i < colheadersdata.GetLength(0); i++)
                {
                    C1.WPF.FlexGrid.Row row = new C1.WPF.FlexGrid.Row();
                    FGcolheaders.Rows.Add(row);
                    row.AllowMerging = true;
                }

                //fill row headers
                for (int i = 0; i < colheadersdata.GetLength(0); i++)
                    for (int j = 0; j < colheadersdata.GetLength(1); j++)
                    {
                        if (colheadersdata[i, j] != null && colheadersdata[i, j].Trim().Equals(".-."))
                            FGcolheaders[i, j] = "";//14Jul2014 filling empty header
                        else
                            FGcolheaders[i, j] = colheadersdata[i, j];
                    }
            }
        }

        private void FillRowHeaders(string[] rowHeaders, AUGrid c1fgrid)
        {
            bool isrowheaderchecked = true;// rowheaderscheck.IsChecked == true ? true : false;
            //creating row headers
            string[,] rowheadersdata = new string[rowHeaders.Length, 1];
            ////creating data
            for (int r = 0; r < rowheadersdata.GetLength(0); r++)
            {
                for (int c = 0; c < rowheadersdata.GetLength(1); c++)
                {
                    rowheadersdata[r, c] = rowHeaders[r];
                }
            }
            //create & fill row headers
            bool fillrowheaders = isrowheaderchecked;
            if (fillrowheaders)
            {
                var FGrowheaders = c1fgrid.RowHeaders;
                FGrowheaders.Columns[0].AllowMerging = true;
                FGrowheaders.Columns[0].VerticalAlignment = VerticalAlignment.Top;
                FGrowheaders.Columns[0].Width = new GridLength(70);

                for (int i = FGrowheaders.Columns.Count; i < rowheadersdata.GetLength(1); i++)
                {
                    C1.WPF.FlexGrid.Column col = new C1.WPF.FlexGrid.Column();
                    col.AllowMerging = true;
                    col.VerticalAlignment = VerticalAlignment.Top;
                    col.Width = new GridLength(70);
                    FGrowheaders.Columns.Add(col);
                }

                for (int i = FGrowheaders.Rows.Count; i < rowheadersdata.GetLength(0); i++)
                {
                    C1.WPF.FlexGrid.Row row = new C1.WPF.FlexGrid.Row();
                    row.AllowMerging = true;
                    FGrowheaders.Rows.Add(row);
                }

                //fill row headers
                for (int i = 0; i < rowheadersdata.GetLength(0); i++)
                    for (int j = 0; j < rowheadersdata.GetLength(1); j++)
                    {
                        if (rowheadersdata[i, j] != null && rowheadersdata[i, j].Trim().Equals(".-."))
                            FGrowheaders[i, j] = "";//14Jul2014 filling empty header
                        else
                            FGrowheaders[i, j] = rowheadersdata[i, j];
                    }
            }
        }
        #endregion

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
                fullpathfilename.Text = saveasFileDialog.FileName;
            }
        }

        public void PasteSyntax(string command)//29Jan2013
        {
            string newlines = (inputTextbox.Text != null && inputTextbox.Text.Trim().Length > 0) ? "\n\n" : string.Empty;
            if (command != null && command.Length > 0)
                inputTextbox.AppendText(newlines + command);// inputTextbox.Text = existingCommands + command;
            int linecount = inputTextbox.LineCount - 1;
            inputTextbox.ScrollToLine(linecount); // linecount is zero based so decremented by 1 in above line
            inputTextbox.Select(inputTextbox.Text.Length, 0);//placing cursor at the end
        }

        //Not in Use
        private void save_Click(object sender, RoutedEventArgs e)
        {
            //////// Active output window ///////
            OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
            OutputWindow ow = owc.ActiveOutputWindow as OutputWindow; //get currently active window
            if (saveoutput.IsChecked == true)
                ow.DumpAllAnalyisOuput(fullfilepathname, fileformat, extratags);
        }

        private bool IsValidFullPathFilename(string path, bool filemustexist)
        {
            bool validDir, validFile;
            string message = string.Empty;
            string dir = Path.GetDirectoryName(path);
            string filename = Path.GetFileName(path);

            ///Check filename
            if (filemustexist && !File.Exists(path))// || (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                validFile = false;
                message = "Invalid Filename! " + filename;
            }
            else
                validFile = true;

            //// Check Directory path
            if (Directory.Exists(dir) || dir.Trim().Length == 0)// valid folder or blank if defaults are needed
            {
                validDir = true;
            }
            else
            {
                message = message + " Invalid Directory path! " + dir;
                validDir = false;
            }
            if (message.Trim().Length > 0)
            {
                logService.Warn(message);
            }
            return (validDir && validFile);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)//Do this only if Syn Edtr is visible.
            {
                this.Activate();
                System.Windows.Forms.DialogResult dresult = System.Windows.Forms.DialogResult.OK;
                if (Modified)
                {
                    dresult = System.Windows.Forms.MessageBox.Show(
                              "Do you want to save commands before closing Command Editor?",
                              "Save & Exit Command Editor?",
                              System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                              System.Windows.Forms.MessageBoxIcon.Question);
                }
                if (dresult == System.Windows.Forms.DialogResult.Cancel)//dont close
                {
                    e.Cancel = true; // do not close this window
                    this.SEForceClose = false;
                }
                else /// [just hide. OR hide and close] with or without saving
                {
                    ///before closing save R scripts in Syntax Editor text area..13Feb2013
                    if (dresult == System.Windows.Forms.DialogResult.Yes)//Save
                        SyntaxEditorSaveAs();

                    /// Do hide OR hide & close ///
                    inputTextbox.Text = string.Empty; //Clean window
                    this.Visibility = System.Windows.Visibility.Hidden;// hide window. If you close down you can't reopen it again.
                    if (!this.SEForceClose)
                        e.Cancel = true;//stop closing
                    //else we Forcefully close this window. We dont want to open it again, Since application is exiting.
                    Modified = false;
                    Title = "Command Editor Window";
                }
            }

        }

        //New : clears the command area
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Modified)//if any modification done to command scripts after last Save
            {  //allow user to save changes before opening another command script
                System.Windows.Forms.DialogResult dresult = System.Windows.Forms.MessageBox.Show(
                          "Do you want to save commands?",
                          "Save & Close current command script?",
                          System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                          System.Windows.Forms.MessageBoxIcon.Question);
                if (dresult == System.Windows.Forms.DialogResult.Yes)//Yes Save- and Close
                {
                    SyntaxEditorSaveAs();
                }
                else if (dresult == System.Windows.Forms.DialogResult.No)//No Save- but Close
                {
                }
                else//no Save no Close
                {
                    return;
                }
            }
            inputTextbox.Text = string.Empty;
            this.Activate();
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            SyntaxEditorOpen();
            this.Activate();//19Feb2013
        }

        private void MenuItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SyntaxEditorSaveAs();
            this.Activate();//19Feb2013
        }

        private void SyntaxEditorOpen()
        {
            if (Modified)//if any modification done to command scripts after last Save
            {  //allow user to save changes before opening another command script
                System.Windows.Forms.DialogResult dresult = System.Windows.Forms.MessageBox.Show(
                          "Do you want to save commands before loading new script?",
                          "Save & Close current command script?",
                          System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                          System.Windows.Forms.MessageBoxIcon.Question);
                if (dresult == System.Windows.Forms.DialogResult.Yes)//Yes Save- and Close
                {
                    SyntaxEditorSaveAs();
                }
                else if (dresult == System.Windows.Forms.DialogResult.No)//No Save- but Close
                {
                }
                else//no Save no Close
                {
                    return;
                }
            }
            const string FileNameFilter = "BSky R scripts, (*.bsr)|*.bsr"; //BSkyR
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = FileNameFilter;
            bool? output = openFileDialog.ShowDialog(Application.Current.MainWindow);
            if (output.HasValue && output.Value)
            {
                System.IO.StreamReader file = new System.IO.StreamReader(openFileDialog.FileName);
                inputTextbox.Text = file.ReadToEnd();
                file.Close();
            }
            Modified = false;//19Feb2013 Newly loaded script can only be modified after loading finishes.
            Title = "Command Editor Window"; //19Feb2013
        }

        private void SyntaxEditorSaveAs()
        {
            const string FileNameFilter = "BSky R scripts, (*.bsr)|*.bsr"; //BSkyR
            SaveFileDialog saveasFileDialog = new SaveFileDialog();
            saveasFileDialog.Filter = FileNameFilter;
            bool? output = saveasFileDialog.ShowDialog(Application.Current.MainWindow);
            if (output.HasValue && output.Value)
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter(saveasFileDialog.FileName);
                file.WriteLine(inputTextbox.Text);
                file.Close();
            }
            Modified = false;//19Feb2013 currently saving. So immediately after save there are no new changes/modifications.
            Title = "Command Editor Window"; //19Feb2013
        }

        //19Feb2013 If anybody edits/changes something in text-area
        private void inputTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Modified = true;
            Title = "Command Editor Window - < unsaved script >";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDatagrids();
        }

        //17May2013
        public void ShowWindowLoadFile()
        {
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(DoubleClickedFilename);
                inputTextbox.Text = file.ReadToEnd();
                file.Close();
                Modified = false;
                Title = "Command Editor Window";
                this.Show();
            }
            catch
            {
                logService.WriteToLogLevel("Error Opening file by double click", LogLevelEnum.Error);
            }
        }
    }

    public class ImageDimensions
    {
        public int imagewidth { get; set; }
        public int imageheight { get; set; }

        public bool overrideImgDim
        {
            get
            {
                if (imagewidth > -1 && imageheight > -1) //if valid dimentsion are set, means override the values set through 'options'.
                {
                    return true;
                }
                else // if image dimenstions are invalid, means use the defaults from options
                {
                    return false;
                }

            }
        } 
    }
}
