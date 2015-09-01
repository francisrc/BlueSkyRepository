using System.Windows;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Controls;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using System.Windows.Media;
using System;
using System.Reflection;
using System.Collections.Generic;
using BlueSky.Commands.Tools.Package;
using BSky.Statistics.Common;
using BlueSky.Commands.Tools.Package.Dialogs;
using BSky.Statistics.Service.Engine.Interfaces;

namespace BlueSky.Windows
{
    /// <summary>
    /// Interaction logic for CustomSettingsWindow.xaml
    /// </summary>
    public partial class CustomSettingsWindow : Window
    {
        IAnalyticsService analytics = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();//18Sep2014
        IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//18Sep2014
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//18Sep2014

        public CustomSettingsWindow()
        {
            InitializeComponent();
            LoadCustom();
            this.DataContext = AllAppSettings;
            hasGraphicImageSizeEdited = false;//this is important
            //LoadColorListBox();
        }
        NameValueCollection AllAppSettings;
        IConfigService conService;

        private void LoadDefaults()//for defaulting to factory settings
        {
            string tempDir = System.IO.Path.GetTempPath();//C:\Users\Anil\AppData\Local\Temp\
        }

        private void LoadCustom() //for user's custom settings
        {
            conService = LifetimeService.Instance.Container.Resolve<IConfigService>();
            conService.LoadConfig();//load new settings
            AllAppSettings = conService.AppSettings;
            //tempfolder.Text = conService.AppSettings.Get("tempfolder");
        }


        //For now PathTab, ColorTab, UsrPkgTab will use following function. Later we can decide to divide different set of options
        // matching their tabs. Right now we are setting all no matter which of the 3 tab is active.
        private void ApplyChanges()
        {
            if (IsValidDirectory(tempfolder.Text))// or blank if defaults are needed
            {
                conService.RefreshConfig();
                //if (!conService.Success)
                //    MessageBox.Show(conService.Message);
            }
        }

        private void tempfolderbrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowseDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowseDialog.SelectedPath = tempfolder.Text != null ? tempfolder.Text : string.Empty;
            System.Windows.Forms.DialogResult result = folderBrowseDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (IsValidDirectory(folderBrowseDialog.SelectedPath))// or blank if defaults are needed
                {
                    string unixpath = folderBrowseDialog.SelectedPath.Replace('\\', '/');
                    AllAppSettings.Set("tempfolder", unixpath);//folderBrowseDialog.SelectedPath);
                    //tempfolder.Text = folderBrowseDialog.SelectedPath;
                    //OR
                    tempfolder.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                }
            }
        }

        private void initialfolderbrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowseDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowseDialog.SelectedPath = test.Text != null ? test.Text : string.Empty;
            System.Windows.Forms.DialogResult result = folderBrowseDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (IsValidDirectory(folderBrowseDialog.SelectedPath))// or blank if defaults are needed
                {
                    string unixpath = folderBrowseDialog.SelectedPath.Replace('\\', '/');
                    AllAppSettings.Set("test", unixpath);//folderBrowseDialog.SelectedPath);
                    //tempfolder.Text = folderBrowseDialog.SelectedPath;
                    //OR
                    test.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                }
            }
        }

        //set R Home
        private void rhomebrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowseDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowseDialog.SelectedPath = rhome.Text != null ? rhome.Text : string.Empty;
            System.Windows.Forms.DialogResult result = folderBrowseDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (IsValidDirectory(folderBrowseDialog.SelectedPath))// or blank if defaults are needed
                {
                    string unixpath = folderBrowseDialog.SelectedPath.Replace('\\', '/');
                    AllAppSettings.Set("rhome", unixpath);//folderBrowseDialog.SelectedPath);
                    //rhome.Text = folderBrowseDialog.SelectedPath;
                    //OR
                    rhome.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                }
            }
        }

        private void LoadColorListBox()
        {

            StackPanel sp = null;
            TextBox tbx = null;
            TextBlock tb = null;

            Type colorType = typeof(System.Windows.Media.Color);
            // We take only static property to avoid properties like Name, IsSystemColor ...
            PropertyInfo[] propInfos = colorType.GetProperties(BindingFlags.Public);
            foreach (PropertyInfo propInfo in propInfos)
            {
                //Console.WriteLine(propInfo.Name);

                sp = new StackPanel(); sp.Orientation = Orientation.Horizontal;//new SolidColorBrush(Colors.Blue);
                tbx = new TextBox(); tbx.Width = 10; tbx.Height = 10; tbx.Background =  new SolidColorBrush(Colors.Blue);
                tb = new TextBlock(); tb.Text = propInfo.Name;// Colors.Blue.ToString();
                sp.Children.Add(tbx); sp.Children.Add(tb);
               // colorlistbox.Items.Add(sp);
            }
        }

        private void color_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string controlname = (sender as FrameworkElement).Name; //This name must match to key in config file.
            System.Windows.Shapes.Rectangle r = (System.Windows.Shapes.Rectangle)sender;
            
                        //Get Current color
            SolidColorBrush scb = r.Fill as SolidColorBrush;
            var DrColor = System.Drawing.Color.FromArgb(scb.Color.A,scb.Color.R,scb.Color.G,scb.Color.B);

                        //WPF RGB color slider
            ColorSelectorWindow csw = new ColorSelectorWindow();
            //csw.OldColor = scb;//new SolidColorBrush((color.Background as Brush).);
            //csw.ShowDialog();
            //r.Fill = csw.CurrentColor;
            //AllAppSettings.Set(controlname, csw.HexColor);

                        //Windows Forms color picker tool
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
            cd.FullOpen = true;
            cd.Color = DrColor;
            cd.ShowDialog();
            System.Windows.Media.Color selcolor = new System.Windows.Media.Color();
            selcolor.A = cd.Color.A;
            selcolor.R = cd.Color.R;
            selcolor.G = cd.Color.G;
            selcolor.B = cd.Color.B;
            string hexcolor = "#FF" + selcolor.R.ToString("X2") + selcolor.G.ToString("X2") + selcolor.B.ToString("X2");
            r.Fill = new SolidColorBrush(selcolor);
            AllAppSettings.Set(controlname, hexcolor);
            cd.Dispose();
        }


        #region User Session R Packages TAB
        RecentItems userPackageList = LifetimeService.Instance.Container.Resolve<RecentItems>();//06Feb2014
        private void UserPkgTab_Loaded(object sender, RoutedEventArgs e)
        {
            userPackageList.RefreshXMLItems();
            ReloadUserPackages(); // (re)load in listbox
            //ApplyPackageButton.IsEnabled = false;
        }

        private void AddPackageButton_Click(object sender, RoutedEventArgs e)
        {
            #region This code is same as in LoadPackageFRomListCommand.cs in OnExecute()
            //get package name from another window
            PackageHelperMethods phm = new PackageHelperMethods();
            UAReturn rlst = phm.ShowInstalledPackages();
            string[] installedpkgs = phm.GetUAReturnStringResult(rlst);

            UAReturn rlst2 = phm.ShowLoadedPackages();
            string[] loadededpkgs = phm.GetUAReturnStringResult(rlst2);
            string[] strarr = phm.GetStringArrayUncommon(installedpkgs, loadededpkgs);

            //Create UI show list of installed packges so that user can select and load them
            SelectPackagesWindow spw = new SelectPackagesWindow(strarr);
            spw.header = "Select Packages.";
            spw.ShowDialog();
            IList<string> sel = spw.SelectedItems;
            if (sel == null)
                return;

            string[] selectedpackages = new string[sel.Count];
            int i = 0;
            foreach (string s in sel)
            {
                selectedpackages[i] = s;
                i++;
            }
            #endregion

            foreach (string s in selectedpackages)
                packagelistbox.Items.Add(s);
            //ApplyPackageButton.IsEnabled = true;
        }

        private void RemovePackageButton_Click(object sender, RoutedEventArgs e)
        {
            packagelistbox.Items.Remove(packagelistbox.SelectedItem);
            //ApplyPackageButton.IsEnabled = true;
        }

        private void ResetPackageButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadUserPackages();
            //ApplyPackageButton.IsEnabled = true;
        }

        private void ApplyPackageButton_Click(object sender, RoutedEventArgs e)
        {
            SaveUserPackageList();
            SaveDefaultPackageList();
        }

        //Save option for User Packages tab
        private void SaveUserPackageList() // this is not saved with config xml file but else where. So saving is different for this tab.
        {
            userPackageList.RemoveAllItems();
            string[] itemsarr = new string[packagelistbox.Items.Count];
            int i = 0;
            foreach (object item in packagelistbox.Items)
            {
                itemsarr[i] = (item.ToString());
                i++;
            }
            userPackageList.AddXMLItems(itemsarr);
            //ApplyPackageButton.IsEnabled = false;
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            int selidx = packagelistbox.SelectedIndex;
            if (selidx >= 0 && selidx < packagelistbox.Items.Count - 1) // if no item selected OR not the last item
            {
                var itm = packagelistbox.SelectedItem;
                packagelistbox.Items.Remove(itm);
                packagelistbox.Items.Insert(selidx + 1, itm);
                packagelistbox.SelectedItem = itm;
                //ApplyPackageButton.IsEnabled = true;
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            int selidx = packagelistbox.SelectedIndex;
            if (selidx > 0 && selidx <= packagelistbox.Items.Count - 1) // if not the first item
            {
                var itm = packagelistbox.SelectedItem;
                packagelistbox.Items.Remove(itm);
                packagelistbox.Items.Insert(selidx - 1, itm);
                packagelistbox.SelectedIndex = selidx - 1;
                //ApplyPackageButton.IsEnabled = true;
            }
        }

        private void ReloadUserPackages()
        {
            packagelistbox.Items.Clear();
            List<string> items = userPackageList.RecentFileList;
            foreach (string item in items)
                packagelistbox.Items.Add(item);
        }

        #endregion

        #region Common Functions 
        
        private void CancelBut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DefaultBut_Click(object sender, RoutedEventArgs e)
        {
            conService.SetAllSettingsToDefault();
            if (!conService.Success)
                MessageBox.Show(conService.Message);
            this.Close();
        }

        // This is common Apply Button for all tabs.
        // Further we may call respective save/apply functions, for the tab that is currently active.
        private void ApplyBut_Click(object sender, RoutedEventArgs e)
        {
            string seltab = GetActiveTabName();
            switch (seltab)
            {
                case "PathTab":
                    ApplyChanges();
                    break;
                case "ColorsTab":
                    ApplyChanges();
                    break;
                case "DefaultPkgTab":
                    SaveDefaultPackageList();
                    break;
                case "UserPkgTab":
                    SaveUserPackageList();
                    break;
                case "OthersTab":
                    ApplyChanges();
                    break;
                case "ImageTab":
                    ////refresh graphic image size, if modified.
                    //if (hasGraphicImageSizeEdited)
                    //{
                    //    RefreshSyntaxGraphicSize();
                    //    hasGraphicImageSizeEdited = false;
                    //}
                    ApplyChanges();
                    break;
                case "AdvTab":
                    ApplyChanges();
                    break;
                default:
                    MessageBox.Show("Wrong Tab Option Found!!");
                    break;
            }

            //18Sep2014//Set Decimal digit so that it can be used immediately after setting window closes
            if (noofdecimals.Text != null && noofdecimals.Text.Length > 0)
            {
                CommandRequest rcmd = new CommandRequest();
                //Call R function to get Decimal digit
                rcmd.CommandSyntax = "BSkyGetDecimalDigitSetting()"; // Get Decimal Digit
                object retres = analytics.ExecuteR(rcmd, true, false);
                string rdeci = retres != null ? retres.ToString() : string.Empty;
                //MessageBox.Show("RDeci: " + rdeci);

                int res;
                bool parsed = Int32.TryParse(rdeci, out res);
                int decidigit = parsed ? res : 2;//is parsed successfully default will be res else default will be 2.

                string deci = noofdecimals.Text!=null ? noofdecimals.Text:string.Empty ;
                bool isSet= Int32.TryParse(deci, out decidigit);
                //MessageBox.Show("Deci: " + deci);

                //Call R function to set Decimal digit
                rcmd.CommandSyntax = "BSkySetDecimalDigitSetting(decimalDigitSetting = " + decidigit + ")"; //Set Decimal Digit
                retres = analytics.ExecuteR(rcmd, false, false);
            }

            //05Jul2015//Set scientific notaion flag in R
            bool isSciNotation = (scientific.IsChecked == true);
            if (true)
            {
                string CapBoolStr = (isSciNotation)?"TRUE": "FALSE";
                CommandRequest rcmd = new CommandRequest();
                //Call R function to set Scientific Notaion flag
                rcmd.CommandSyntax = "BSkySetEngNotationSetting( " + CapBoolStr + ")"; //Set Scientific Notation
                object retres = analytics.ExecuteR(rcmd, false, false);
            }

            //refresh graphic image size, if modified.
            if (hasGraphicImageSizeEdited)
            {
                RefreshSyntaxGraphicSize();
                hasGraphicImageSizeEdited = false;
            }
            this.Close();// close Options Window
        }


        //Find active tab name property
        private string GetActiveTabName()
        {
            TabItem ti = GetActiveTabItem();
            if (ti != null)
            {
                return (ti.Name);
            }
            else
                return string.Empty;
        }
        
        //Find Active Tab
        private TabItem GetActiveTabItem()
        {
            TabItem ti = options.SelectedItem as TabItem;
            return ti;
        }

        private bool IsValidDirectory(string path)
        {
            if (Directory.Exists(path) || path.Trim().Length == 0)// valid folder or blank if defaults are needed
            {
                return true;
            }
            else
            {
                MessageBox.Show("Path does not exists! " + path);
                return false;
            }
        }


        #endregion


        #region default packages
        XMLitemsProcessor defaultPackageList = LifetimeService.Instance.Container.Resolve<XMLitemsProcessor>();//06Feb2014
        private void DefltPkgTab_Loaded(object sender, RoutedEventArgs e)
        {
            defaultPackageList.RefreshXMLItems();
            ReloadDefaultPackages(); // (re)load in listbox
            ////ApplyPackageButton.IsEnabled = false;
        }

        private void dfltMoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            int selidx = dfltpackagelistbox.SelectedIndex;
            if (selidx > 0 && selidx <= dfltpackagelistbox.Items.Count - 1) // if not the first item
            {
                var itm = dfltpackagelistbox.SelectedItem;
                dfltpackagelistbox.Items.Remove(itm);
                dfltpackagelistbox.Items.Insert(selidx - 1, itm);
                dfltpackagelistbox.SelectedIndex = selidx - 1;
                //ApplyPackageButton.IsEnabled = true;
            }
        }

        private void dfltMoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            int selidx = dfltpackagelistbox.SelectedIndex;
            if (selidx >= 0 && selidx < dfltpackagelistbox.Items.Count - 1) // if no item selected OR not the last item
            {
                var itm = dfltpackagelistbox.SelectedItem;
                dfltpackagelistbox.Items.Remove(itm);
                dfltpackagelistbox.Items.Insert(selidx + 1, itm);
                dfltpackagelistbox.SelectedItem = itm;
                ////ApplyPackageButton.IsEnabled = true;
            }
        }

        private void dfltAddPackageButton_Click(object sender, RoutedEventArgs e)
        {
            #region This code is same as in LoadPackageFRomListCommand.cs in OnExecute()
            //get package name from another window
            PackageHelperMethods phm = new PackageHelperMethods();
            UAReturn rlst = phm.ShowInstalledPackages();
            string[] installedpkgs = phm.GetUAReturnStringResult(rlst);

            UAReturn rlst2 = phm.ShowLoadedPackages();
            string[] loadededpkgs = phm.GetUAReturnStringResult(rlst2);
            string[] strarr = phm.GetStringArrayUncommon(installedpkgs, loadededpkgs);

            //Create UI show list of installed packges so that user can select and load them
            SelectPackagesWindow spw = new SelectPackagesWindow(strarr);
            spw.header = "Select Packages.";
            spw.ShowDialog();
            IList<string> sel = spw.SelectedItems;
            if (sel == null)
                return;

            string[] selectedpackages = new string[sel.Count];
            int i = 0;
            foreach (string s in sel)
            {
                selectedpackages[i] = s;
                i++;
            }
            #endregion

            foreach (string s in selectedpackages)
                dfltpackagelistbox.Items.Add(s);
            //ApplyPackageButton.IsEnabled = true;
        }

        private void dfltRemovePackageButton_Click(object sender, RoutedEventArgs e)
        {
            dfltpackagelistbox.Items.Remove(dfltpackagelistbox.SelectedItem);
            //ApplyPackageButton.IsEnabled = true;
        }

        private void dfltResetPackageButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadDefaultPackages();
            //ApplyPackageButton.IsEnabled = true;
        }

        private void ReloadDefaultPackages()
        {
            dfltpackagelistbox.Items.Clear();
            List<string> items = defaultPackageList.RecentFileList;
            foreach (string item in items)
                dfltpackagelistbox.Items.Add(item);
        }

        //Save option for User Packages tab
        private void SaveDefaultPackageList() // this is not saved with config xml file but else where. So saving is different for this tab.
        {
            defaultPackageList.RemoveAllItems();
            string[] itemsarr = new string[dfltpackagelistbox.Items.Count];
            int i = 0;
            foreach (object item in dfltpackagelistbox.Items)
            {
                itemsarr[i] = (item.ToString());
                i++;
            }
            defaultPackageList.AddXMLItems(itemsarr);
            //ApplyPackageButton.IsEnabled = false;
        }


        #endregion



        bool isSyntaxGraphicRefreshed = false;
        private void RefreshSyntaxGraphicSize()
        {
            //image height width edited. Set flag in Syntax telling to refresh image dementions.
            //Launch Syntax Editor window with command pasted /// 29Jan2013
            if (!isSyntaxGraphicRefreshed) //refresh once
            {
                //MainWindow mwindow = LifetimeService.Instance.Container.Resolve<MainWindow>();
                ////// Get Syntax Editor  //////
                SyntaxEditorWindow sewindow = LifetimeService.Instance.Container.Resolve<SyntaxEditorWindow>();
                //sewindow.Owner = mwindow;
                sewindow.RefreshImgSizeForGraphicDevice();
                isSyntaxGraphicRefreshed = true;
            }
        }

        bool hasGotFocusBefore = false;
        private void TextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            //when first time 'Image' tab is clicked, this 'if' block should run only once, till the ConfigWindow is open.
            if (!hasGotFocusBefore)
            {
                //set it to false because you are in Image tab very first time so basically you haven't changed any value yet.
                hasGraphicImageSizeEdited = false;
            }
            hasGotFocusBefore = true;//for this session of ConfigWindow do not allow 'if' block to run again.
        }

        bool hasGraphicImageSizeEdited;
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            hasGraphicImageSizeEdited = true;//set to true if image size fields are edited
        }


    }
}
