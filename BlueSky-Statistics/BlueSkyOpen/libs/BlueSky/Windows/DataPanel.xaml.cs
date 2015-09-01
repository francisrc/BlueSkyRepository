using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections;
using C1.WPF.DataGrid;
using BSky.Statistics.Common;
using BSky.Statistics.Service.Engine.Interfaces;
using System.Globalization;
using System.Collections.ObjectModel;
using Microsoft.Practices.Unity;
using BSky.Lifetime;
using BSky.Interfaces.Model;
using BSky.XmlDecoder;
using BSky.Interfaces.Interfaces;
using BlueSky.Model;
using System.Reflection;
using System.Text;
using BSky.Lifetime.Interfaces;



namespace BlueSky.Windows
{
    /// <summary>
    /// Interaction logic for DataPanel.xaml
    /// </summary>
    public partial class DataPanel : UserControl
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012

        public DataPanel()
        {
            InitializeComponent();
        }

        DatasetLoadingBusyWindow dslbw = null; //08Aug2013 For showing busy message while background processing is in progress
         
        private DataSource ds;//A. to get ref. of current datasource

        public DataSource DS
        {
            get { return ds; }
            set { ds = value; }
        }

        private IList data;

        public IList Data
        {
            get { return data; }
            set
            {
                data = value;
                gridControl1.ItemsSource = null;
                gridControl1.AutoGenerateColumns = true;
                gridControl1.ItemsSource = data;
                gridControl1.CanUserAddRows = true;
                gridControl1.CanUserEditRows = true;
            }
        }

        private ObservableCollection<DataSourceVariable> variables;
        //private IList variables;
        public IList Variables
        {
            get { return variables; }
            set
            {
                variables = new ObservableCollection<DataSourceVariable>(value as List<DataSourceVariable>);
                //variables = value;
                variableGrid.ItemsSource = variables;
            }
        }

        public List<string>  sortcolnames { get; set; } //11Apr2014 for sorted column names so that we can display sort icon for that col.
        public string sortorder { get; set; }//14Apr2014 for choosing right icon for sorting based on ascending/descending order;

        #region Variablegrid
        /// Events related to varaible grid ////

        private string rowid;
        private int rowindex;
        private int varcount = 1;

        private void variableGrid_BeginningEdit(object sender, C1.WPF.DataGrid.DataGridBeginningEditEventArgs e)
        {
            //MessageBox.Show("Beginning Edit..");// When you click on cel to edit it and its getting ready  for edit
            rowid = variableGrid.CurrentCell.Row.DataItem.ToString();//gender
            rowindex = variableGrid.CurrentRow.Index; 
        }

        private void variableGrid_BeganEdit(object sender, DataGridBeganEditEventArgs e)
        {
            //MessageBox.Show("Began Edit..");// now you got focus to type
        }

        //bool committedVarCell = false;
        private void variableGrid_CommittingEdit(object sender, DataGridEndingEditEventArgs e)
        {
            //if (committedVarCell)
            //{
            //    e.Cancel = true;
            //    return;
            //}
            //committedVarCell = true;
            // //on cell Edit and clicking elsewhere gives the info about edited cell
            List<string> colLevels = null;
            string cellVal = variableGrid.CurrentCell.Text;//eg..Male or Female
            string cellValue = cellVal != null ? cellVal.Replace("'", @"\'").Replace("\"", @"\'") : string.Empty;
            //string cellValue = cellVal != null ? cellVal.Replace("'", @"\'") : string.Empty;
            if (cellValue == null || cellValue.Trim().Length < 1) //Do not create new variable row if variable name is not provided
            {
                //method1 variableGrid.RemoveRow(variableGrid.CurrentRow.Index);

                //variableGrid.RaiseEvent();
                return;
            }
            //rowid = variableGrid.CurrentCell.Row.DataItem.ToString();//eg..gender // should be captured when we click on cell
            string colid = variableGrid.CurrentCell.Column.Header.ToString();//eg..Label
            switch (e.Column.Name)
            {
                case "Name":
                    break;
                case "DataType":
                    if (colid.Equals("DataType")) colid = "Type"; // colid must match with R side property name. Else it will not work
                    //MessageBox.Show(selectedData);
                    if (cellValue.Equals("String")) cellValue = "character";
                    if (cellValue.Equals("Numeric") || cellValue.Equals("Int") || cellValue.Equals("Float") || cellValue.Equals("Double")) cellValue = "numeric";
                    if (cellValue.Equals("Bool")) cellValue = "logical";
                    break;
                case "Width":
                    break;
                case "Decimals":
                    break;
                case "Label":
                    break;
                case "Values": 
                    colid = "Levels"; // "Levels"  new property name in R code
                    break;

                case "Missing":
                    break;
                case "Columns":
                    break;
                case "Alignment": 
                    colid = "Align"; // "Align" new property name in R code
                    C1.WPF.DataGrid.DataGridComboBoxColumn col = e.Column as C1.WPF.DataGrid.DataGridComboBoxColumn;
                    C1.WPF.C1ComboBox combo = e.EditingElement as C1.WPF.C1ComboBox;
                    string value = combo.Text;
                    break;
                case "Measure":
                    colLevels = getLevels();

                    break;
                case "Role":
                    break;
                default:
                    break;

            }

            ///// Modifying R side Dataset ////////
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            if (rowid == null)//new row
            {
                int rowindex = 0;//variableGrid.CurrentRow.Index;
                string datagridcolval = ".";//default value for new col in datagrid view.
                // add new row cellValue is new colname
                analyticServ.addNewVariable(cellValue, datagridcolval, rowindex, ds.Name);


                //// Insert on UI side dataset ///
                DataSourceVariable var = new DataSourceVariable();
                // string RecCount = (this.Variables.Count + 1).ToString();//add 1 because its 0 based
                // int insertrowindex = variableGrid.SelectedIndex;
                 var.Name = cellValue; //////// Check Problem for manually appending new Var at the end
                 DS.Variables.Add(var);
                //this.Variables.Insert(rowindex, var);
                //DS.Variables.Insert(rowindex, var);//one more refresh needed. I guess
                //renumberRowHeader(variableGrid);
            }
            else
            {//edit existing row
                UAReturn retval = analyticServ.EditVarGrid(ds.Name, rowid, colid, cellValue, colLevels);
                retval.Success = true;
                ///08Jul2013 Show Error/Warning in output window if any.
                //if (retval != null && retval.Data != null)
                //    SendErrorWarningToOutput(retval);
            }
            //variableGrid. = cellValue;
            //MessageBox.Show("[R:"+rowid + "] [C:" + colid + "] [V:" + cellValue + "] [DS:" + ds.Name+"]");
            ds.Changed = true;
            if (e.Column.Name.Equals("Name"))
            refreshDataGrid();
            //arrangeVarGridCols();//rearrange the var-grid cols

        }

        private void variableGrid_SelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
        {
            //// on click gives details about the cell
            //string selectedData = "";
            //if (variableGrid.CurrentCell == null)
            //    return;
            //string cellValue = variableGrid.CurrentCell.Text;
            //rowid = variableGrid.CurrentCell.Row.DataItem.ToString();
            //string colid = variableGrid.CurrentCell.Column.Header.ToString();
            //selectedData += rowid + ":" + colid + ": " + cellValue + "\n";
            ////MessageBox.Show(selectedData);
        }

        #region value label popup dialog

        private List<string> getLevels()
        {
            string cellValue = variableGrid.CurrentCell.Text;
            string rowid = variableGrid.CurrentCell.Row.DataItem.ToString();
            // check this string colid = variableGrid.CurrentCell.Column.Header.ToString();

            /////testing Value Lable popup /////22Sep2011
            foreach (var v in ds.Variables)//search for col name
            {
                if (v.Name.Equals(rowid))//if colname is found
                {
                    return (v.Values);
                }
            }
            return (null);
        }

        private void valueLabelDialog()
        {

            string selectedData = "";
            string cellValue = variableGrid.CurrentCell.Text;
            string rowid = variableGrid.CurrentCell.Row.DataItem.ToString();
            // check this string colid = variableGrid.CurrentCell.Column.Header.ToString();

            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();

            bool hasmap = false;
            bool varfound = false;//variable found in ds.Variables or not
            int varidx = 0; // for index of vaiable in ds.Variables. Which is under Scale 2 Nominal (or vice versa ) change.
            /////testing Value Lable popup /////22Sep2011
            ValueLablesDialog fm = new ValueLablesDialog();
            fm.colName = rowid;
            fm.datasetName = ds.Name;
            fm.maxfactors = ds.maxfactor;//setting maximum factor limit.
            string[] dsvals = null;//=new string[ds.Variables.Count];
            DataColumnMeasureEnum measure = DataColumnMeasureEnum.Scale;
            int i = 0;
            ValueLabelDialogMatrix vlmatrix = new ValueLabelDialogMatrix();
            foreach (var v in ds.Variables)//search for col name
            {
                if (v.Name.Equals(rowid))
                {
                    varfound = true;
                    if (v.Values != null && v.Values.Count > 0)//if colname is found
                    {
                        //find if '.' exists. Following can be temp fix. We can refine.
                        bool isdot = false;
                        bool isblank = false; //18Mar2014
                        int unwanteditems = 0;
                        foreach (var lb in v.Values)
                        {
                            if (lb.ToString().Equals("."))
                            { unwanteditems++; }
                            if (lb.ToString().Trim().Equals(""))
                            { unwanteditems++; }
                        }
                        //if(isdot)
                        dsvals = new string[v.Values.Count - unwanteditems];// '.' & '' is excluded
                        //else
                        //    dsvals = new string[v.Values.Count];// '.' does not exists. no need to exclude


                        //dsvals = new string[v.Values.Count-1];// '.' is excluded
                        foreach (var lbls in v.Values)//get value lables for a column
                        {
                            if (!lbls.ToString().Equals(".") && !lbls.ToString().Trim().Equals("")) // dot should be shown in value lable dialog
                            {
                                dsvals[i] = lbls.ToString();
                                vlmatrix.addLevel(lbls.ToString(), i, true);

                                //right now we only support converting first Scale to N/O and then from that N/O to S
                                //Using following 'if' and may be couple fixes we may be able to convert first N/O to scale and then backwards
                                //02feb2014. When converting from Nominal/Ordinal to scale very first time.
                                //Originally variable was Nominal/Ordinal and first time we want to change it to scale.
                                //if (v.factormapList.Count < v.Values.Count)
                                //{
                                //    v.factormapList = new List<FactorMap>();
                                //    FactorMap fmp = null;
                                //    fmp = new FactorMap();
                                //    fmp.labels = lbls.ToString();
                                //    fmp.textbox = lbls.ToString();
                                //    v.factormapList.Add(fmp);
                                //}
                            }
                            i++;
                        }
                        measure = v.Measure;

                        fm.ValueLableListBoxValues = dsvals;//setting in popup
                        fm.oldfactcount = dsvals.Length;

                        //17Apr2014 // for retrieveing stored factor map
                        if (v.factormapList.Count > 0)
                            hasmap = true;
                        //break;
                    }
                    break;
                }
                if (!varfound)
                    varidx++;
            }


            fm.colMeasure = measure;
            fm.changeFrom = measure.ToString();//this 'changeFrom' should not change till dialog is closed(OK/CANCEL)
            fm.OKclicked = false;
            fm.modified = false; // modification done or not
            fm.vlmatrix = vlmatrix; // sending refrence of matrix.
            fm.ShowDialog();


            bool isOkclick = fm.OKclicked;
            bool ismodified = fm.modified;


            if (hasmap && fm.changeTo=="Scale")//17Apr2014 retrieveing stored factor map 
            {
                foreach (FactorMap fcm in ds.Variables[varidx].factormapList)
                {
                    FactorMap cpyfm = new FactorMap();
                    cpyfm.labels = fcm.textbox; //reversing textbox and labels
                    cpyfm.textbox = fcm.labels;
                    fm.factormapList.Add(cpyfm);
                }
            }
            List<FactorMap> fctmaplst = fm.factormapList;
            measure = fm.colMeasure;//retrieve new Measure from Value Lable Dialog
            bool OK_subdialog=false;
            if (isOkclick)
            {
                if (fctmaplst != null && fctmaplst.Count <= DS.maxfactor)//OK from main dailog. Sub dialog will appear now.
                {
                    fm.Close();//release resources held by this popup
                    //show sub dialog
                    ValueLabelsSubDialog vlsd=null;
                    if(fm.changeFrom == "Scale")
                        vlsd = new ValueLabelsSubDialog(fctmaplst, "Existing Values", "New Labels");
                    else
                        vlsd = new ValueLabelsSubDialog(fctmaplst, "Existing Values", "New Labels");// reverse text and labels

                    vlsd.ShowDialog();
                    OK_subdialog = vlsd.OKclicked;
                    fctmaplst = vlsd.factormap;
                    vlsd.Close(); //release resources held by this popup
                    if (OK_subdialog)//ok from sub-dialog
                    {
                        //ds.Variables[varidx].factormapList.Clear();
                        if (fm.changeFrom == "Scale")
                        {
                            //// read changes from UI and set UI vars to take changes //// Update UI side datasource
                            List<string> vlst = new List<string>();
                            foreach (FactorMap newlvl in fctmaplst)//get value lables for a column
                            {
                                if (!newlvl.textbox.Trim().Equals(""))//blanks are ignored from sub-dialog
                                    vlst.Add(newlvl.textbox);
                            }
                            updateVargridValuesCol(rowid, measure, vlst); // update Values Col using common function


                            //17Apr2014 Saving factormap along with other col porps of DataSourceVariable
                            int varcount = ds.Variables.Count;
                            //for (int idx = 0; idx < varcount; idx++)// v in ds.Variables)//no need of 'for' when varidx can do the job
                            //{
                            if (ds.Variables[varidx].Name.Equals(rowid) && ds.Variables[varidx].Values!=null && ds.Variables[varidx].Values.Count > 0)//if colname is found
                                {
                                    ds.Variables[varidx].factormapList.Clear();
                                    foreach (FactorMap fcm in fctmaplst)
                                    {
                                        FactorMap copyfm = new   FactorMap();
                                        copyfm.labels = fcm.labels;
                                        copyfm.textbox = fcm.textbox;

                                        ds.Variables[varidx].factormapList.Add(copyfm);
                                    }
                                    //break; //no need of break, when 'for' is commented out.
                                }
                            //}
                        }
                        else
                            updateVargridValuesCol(rowid, measure, null); // update Values Col using common function
                        if (fm.changeFrom == "Scale" && (fm.changeTo == "Nominal" || fm.changeTo == "Ordinal"))
                        {
                            if (OK_subdialog)//ok from sub-dialog
                            {
                                analyticServ.ChangeScaleToNominalOrOrdinal(rowid, fctmaplst, fm.changeTo, ds.Name);
                            }
                        }
                        else if ((fm.changeFrom == "Nominal" || fm.changeFrom == "Ordinal") && fm.changeTo == "Scale")// Nom, Ord to Scale
                        {
                            //MessageBox.Show("Nom Ord to Scale ");
                            if (OK_subdialog)//ok from sub-dialog
                            {
                                analyticServ.ChangeNominalOrOrdinalToScale(rowid, fctmaplst, fm.changeTo, ds.Name);
                            }
                        }
                    }//OK from sub-dialog
                }

                else // Nominal -> Ordinal  /  Ordinal -> Nominal. No sub dialog, as if now.
                {
                    //check if values are changed and then set ds.Changed = true;
                    if (ismodified)//values changed
                    {
                        ////setting UI side vars and datasets////
                        //measure = fm.colMeasure;//retrieve new Measure from Value Lable Dialog. shifted above for common access
                        List<string> vlst = new List<string>();

                        foreach (string newlvl in fm.ValueLableListBoxValues)//get value lables for a column
                        {
                            vlst.Add(newlvl);
                        }
                        List<ValLvlListItem> finalList = vlmatrix.getFinalList(vlst); 
                         updateVargridValuesCol(rowid, measure, vlst); // update Values Col using common function
                        //set this new list of levels to R for update.
                         analyticServ.ChangeColumnLevels(rowid, finalList, ds.Name);
                    }//if modified
                }//else .. n2s n2o o2n o2s
            }//if OK on main dialog

            fm.Close();//release resources held by this popup

            ////refreshing datagrid. if Main dialog modified or if ok is clicked from sub-dialog
            if (ismodified || OK_subdialog)
            {
                //sortcolnames = null; //10May2014 removesort icon from the already sorted col if its measure is changed.
                refreshDataGrid();
                variableGrid.Refresh();
            }
        }

        private void updateVargridValuesCol(string rowvarname, DataColumnMeasureEnum measure, List<string> vlst)
        {
            ////setting UI side vars and datasets////
            foreach (var v in ds.Variables)//search for col name
            {
                if (v.Name.Equals(rowvarname))//if colname is found
                {
                    //Update UI side datasource
                    v.Measure = measure;
                    v.Values = vlst;
                    ds.Changed = true;
                    break;
                }
            }
        }


        private void valLabel_Click(object sender, RoutedEventArgs e)
        {
            object selectedrow = (sender as FrameworkElement).DataContext; // fixed on 07Feb2014 for New C1 DLLs
            int idx = variableGrid.Rows.IndexOf(selectedrow);
            variableGrid.CurrentRow = variableGrid.Rows[idx];
            ChangeLabels(idx);//05Jun2015 valueLabelDialog();

            //ChangeLabels(variableGrid.SelectedIndex);


            //refreshDataGrid();
            //arrangeVarGridCols();// variableGrid_Loaded(sender, e);//rearrange the var-grid cols
        }
    
        #endregion

        #region missing val popup dialog

        private void missingValueDialog()
        {
            MissingValuesDialog mv = new MissingValuesDialog();
            int i = 0;
            rowid = variableGrid.CurrentCell.Row.DataItem.ToString();//gender
            foreach (var v in ds.Variables)//search for col name
            {
                if (v.Name.Equals(rowid))//if colname is found
                {
                    //mv.vartype = v.DataType;//Data type of column. Can be used for validation in popup.
                    if (v.MissType == null) v.MissType = "none"; //replace null with "none"
                    if (!v.MissType.Equals("none"))
                    {
                        //foreach (var mvls in v.Missing)//get missing values for a column
                        //{
                        //    if (mvls.ToString().IndexOf('=') > 0)
                        //        mvals[i] = mvls.ToString();//setting missing values one by one
                        //    else
                        //    {
                        //        mvals[i] = mvls.ToString();
                        //    }
                        //    i++;
                        //}
                        mv.misvals = v.Missing;//this must come before setting mv.mistype
                        mv.oldmisvals = v.Missing;//keeping an original copy for tracing changes
                    }
                    mv.mistype = v.MissType;// missing type. "none", "three", "range+1"
                    mv.oldMisType = v.MissType;//keeping an original copy for tracing changes
                    break;
                }
            }

            mv.ShowDialog();

            //check if values are changed and then set ds.Changed = true;
            if (mv.OKClicked && mv.isModified)//missing changed
            {
                //mvals = null;
                //mtype = mv.mistype;
                //if (!mv.mistype.Equals("none")) mvals = mv.misvals;
                ///change UI side dataset ////
                foreach (var v in ds.Variables)//search for col name
                {
                    if (v.Name.Equals(rowid))//if colname is found
                    {
                        v.Missing.Clear();
                        if (!mv.mistype.Equals("none"))
                        {
                            v.Missing.AddRange(mv.misvals);
                        }
                        v.MissType = mv.mistype;// missing type. "none", "three", "range+1"
                        break;
                    }
                }

                // Fllowing 3 lines can be removed ... just for display ////
                //string mvals = "";
                //foreach (string st in mv.misvals) mvals = mvals + ":" + st;
                //MessageBox.Show("Ty:" + mv.mistype + ":" + mvals);

                //set this new list of levels to R for update.
                IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
                analyticServ.ChangeMissingVals(rowid, "Missing", mv.misvals, mv.mistype, ds.Name);
                ds.Changed = true;
            }
            mv.Close();//release resources held by this popup
            //variableGrid.Refresh();
            refreshDataGrid();
            variableGrid.Refresh();
        }

        //private void updateVargridMissingCol(string rowvarname, string mistype, List<string> vlst)
        //{
        //    ////setting UI side vars and datasets////
        //    foreach (var v in ds.Variables)//search for col name
        //    {
        //        if (v.Name.Equals(rowvarname))//if colname is found
        //        {
        //            //Update UI side datasource
        //            //v.Missing = measure;
        //            v.MissType = "none";

        //            ds.Changed = true;
        //            break;
        //        }
        //    }
        //}

        private void misval_Click(object sender, RoutedEventArgs e)
        {
            object selectedrow = (sender as FrameworkElement).DataContext; // fixed on 07Feb2014 for New C1 DLLs
            int idx = variableGrid.Rows.IndexOf(selectedrow);
            variableGrid.CurrentRow = variableGrid.Rows[idx];
            //missingValuepop();
            missingValueDialog();
            //refreshDataGrid();
            //arrangeVarGridCols();// variableGrid_Loaded(sender, e);//rearrange the var-grid cols
        }

        #endregion

        //Controlling columns generation. This will set columns based on datatype.Like for Enum Combobox is shown
        // if you want same funtionality thru XAML explore and add proper attributes to achieve this.
        private void variableGrid_AutoGeneratingColumn(object sender, C1.WPF.DataGrid.DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.EditOnSelection = true;//one click edit mode.

            if (e.Property.Name == "Name")
            {
                //e.Column.EditOnSelection = false;
                e.Cancel = false;// generate 'Name' column

            }
            if (e.Property.Name == "DataType")
            {
                e.Column.IsReadOnly = true;
               // e.Column.EditOnSelection = false;//edit col
                e.Cancel = false;

            }
            if (e.Property.Name == "DataClass")
            {
                e.Column.IsReadOnly = true;
                // e.Column.EditOnSelection = false;//edit col
                e.Cancel = false;

            }
            if (e.Property.Name == "Width")
            {
                e.Cancel = true;//hide this

            }
            if (e.Property.Name == "Decimals")
            {
                e.Cancel = true;//hide this

            }
            if (e.Property.Name == "Label")
            {
                e.Column.MaxWidth = 200;// 120;//fixing the width. text field will not expand on typing
                e.Cancel = false;

            }
            if (e.Property.Name == "Values")
            {
                //var comboCol = new C1.WPF.DataGrid.DataGridTemplateColumn();//e.Property

                ////comboCol.ItemsSource = droplst;
                ////comboCol.EditOnSelection = true;

                ////e.Column = comboCol.;
                //e.Column.GroupConverter = new ValueLabelConverter();
                //e.Cancel = false;
                e.Cancel = true;

            }
            if (e.Property.Name == "Missing")
            {
                e.Cancel = true;//dont generate from here

            }

            if (e.Property.Name == "MissType")
            {
                e.Cancel = true;

            }
            if (e.Property.Name == "Columns")
            {
                e.Cancel = true;//hide this

            }
            if (e.Property.Name == "Alignment")
            {
                C1.WPF.DataGrid.DataGridComboBoxColumn col = (C1.WPF.DataGrid.DataGridComboBoxColumn)e.Column;

                List<string> lst = new List<string>();
                lst.Add("Left");
                lst.Add("Right");
                lst.Add("Center");
                col.ItemsSource = lst;
                //col.ItemConverter = new AlignConvertor();
                col.ItemTemplate = this.FindResource("ComboTemplate") as DataTemplate;
                col.Binding.Converter = new AlignConvertor();
                e.Cancel = true;//hide this
            }
            if (e.Property.Name == "Measure")
            {

                C1.WPF.DataGrid.DataGridComboBoxColumn col = (C1.WPF.DataGrid.DataGridComboBoxColumn)e.Column;

                List<string> lst = new List<string>();
                lst.Add("Nominal");
                lst.Add("Ordinal");
                lst.Add("Scale");
                lst.Add("Too Many Levels");
                col.ItemsSource = lst;
                //col.ItemConverter = new AlignConvertor();
                col.ItemTemplate = this.FindResource("ComboTemplate") as DataTemplate;
                col.Binding.Converter = new MeasureConvertor();
                e.Column.IsReadOnly = true;
                e.Cancel = false;

            }
            if (e.Property.Name == "Role")
            {
                //C1.WPF.DataGrid.DataGridComboBoxColumn col = (C1.WPF.DataGrid.DataGridComboBoxColumn)e.Column;

                //List<string> lst = new List<string>();
                //lst.Add("Input");
                //lst.Add("Target");
                //lst.Add("Both");
                //lst.Add("None");
                //lst.Add("Partition");
                //lst.Add("Split");

                //col.ItemsSource = lst;
                //col.ItemTemplate = this.FindResource("ComboTemplate") as DataTemplate;
                //col.Binding.Converter = new RoleConvertor();
                e.Cancel = true; // Make it false and uncomment above all to display this col.

            }
            if (e.Property.Name == "RowCount")
            {
                e.Cancel = true;

            }
            if (e.Property.Name == "ImgURL") // hide this col
            {
                e.Cancel = true;

            }
            if (e.Property.Name == "SortType") //18Apr2013 hide this col
            {
                e.Cancel = true;

            }
            if (e.Property.Name == "factormapList") //17Apr2014 Dont generate this col
            {
                e.Cancel = true;
            }
            if (e.Property.Name == "RName") //19Sep2014 Dont generate this col
            {
                e.Cancel = true;
            }
            if (e.Property.Name == "XName") //19Sep2014 Dont generate this col
            {
                e.Cancel = true;
            }
        }

        private void variableGrid_BeginningNewRow(object sender, DataGridBeginningNewRowEventArgs e)
        {
            //int curRowindex = variableGrid.CurrentRow.Index;
            DataSourceVariable var = new DataSourceVariable();
            //string RecCount = (this.Variables.Count + 1).ToString();//add 1 because its 0 based

            string varname = "newvar";
            //getRightClickRowIndex();
            int rowindex = variableGrid.SelectedIndex;

            //checking duplicate var names
            foreach (DataSourceVariable dsv in this.Variables)
            {
                varname = "newvar" + varcount.ToString();
                if (dsv.Name == varname)
                    varcount++;
            }
            var.Name = varname;
            var.Label = varname;

            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            analyticServ.addNewVariable(var.Name, ".", rowindex + 1, ds.Name);

            this.Variables.Insert(rowindex, var);
            DS.Variables.Insert(rowindex, var);//one more refresh needed. I guess

            renumberRowHeader(variableGrid);
            ds.Changed = true;
            refreshDataGrid(); 
        }

        private void variableGrid_CommittingNewRow(object sender, DataGridEndingNewRowEventArgs e)
        {
        }

        private void variableGrid_CurrentCellChanged(object sender, DataGridCellEventArgs e)
        {
            //MessageBox.Show("Cell Changed");//the moment you click another cell
        }

        private string delcolname;
        private int delvarindex;

        private void variableGrid_DeletingRows(object sender, DataGridDeletingRowsEventArgs e)
        {
            //MessageBox.Show("Del.."+rowid);

                delvarindex = variableGrid.SelectedIndex;
                delcolname = DS.Variables.ElementAt(variableGrid.SelectedIndex).Name; ; // right now assuming single row is to be deleted
            
        }

        private void variableGrid_RowsDeleted(object sender, DataGridRowsDeletedEventArgs e)
        {
            removeVarGridVariable();
        }

        private void removeVarGridVariable()
        {

            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            analyticServ.removeVargridColumn(delcolname, ds.Name);//removing R side
            //renumbering
            renumberRowHeader(variableGrid);
            //remove var in UI side datasets
            DataSourceVariable dsv = new DataSourceVariable();
            dsv = ds.Variables.ElementAt(delvarindex);
            ds.Variables.Remove(dsv);
            //refresh
            refreshDataGrid();
        }

        private void variableGrid_Loaded(object sender, RoutedEventArgs e)
        {
            arrangeVarGridCols();
            HideMouseBusy();
        }

        public void arrangeVarGridCols()
        {
            //MessageBox.Show("Grid Loaded");
            //index of element can be found from XAML. Which ever declared first is index zero, declared second is index 1.
            ///Original sequence:// Value - Missing - Name - DataType - Label - Measure  /////27Mar2015
            variableGrid.Columns.ElementAt(0).DisplayIndex = 5;//width/align/col hidden// 6;//setting Values Col to sixth location. as a sixth col
            variableGrid.Columns.ElementAt(1).DisplayIndex = 5;//width/align/col hidden// 6;
            variableGrid.Columns.ElementAt(5).DisplayIndex = 1;//move measure to second location. //27Mar2015
            //New sequence:  Name - Measure - DataType - Label - Value - Missing  //////27Mar2015

            //variableGrid.Columns.ElementAt(2).DisplayIndex = 8;//setting Alignment col as a 8th col of the grid
        }
       
        private void variableGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            //grid.Columns["ImageUrl"].DisplayIndex = 0;
            //grid.Columns["StandardCost"].DisplayIndex = 6;
            //grid.GroupBy(grid.Columns["ExpirationDate"]);

            //variableGrid.Columns["Values"].DisplayIndex = 6;
            //variableGrid.Columns["Missing"].DisplayIndex = 7;
            //variableGrid.Columns["Alignment"].DisplayIndex = 9;
        }

        private void alignCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
        }

        private void alignCombo_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
        }

        private void variableGrid_LoadedRowHeaderPresenter(object sender, C1.WPF.DataGrid.DataGridRowEventArgs e)
        {
            UpdateRow(e.Row);
        }

        private void variableGrid_CommittedNewRow(object sender, C1.WPF.DataGrid.DataGridRowEventArgs e)
        {
            //MessageBox.Show("New Row Committed");
            //variableGrid.CurrentCell.Text=

            //refreshDataGrid();
        }

        #endregion

        private bool IsNumericStr(string celltxt)
        {
            bool isnumber = false;
            int i=0;

            //foreach (char c in str)
            //{
            //    if (c < '0' || c > '9')
            //        return false;
            //}

            isnumber = int.TryParse(celltxt, out i);
            return isnumber;
        }

        #region Datagrid
        /// Events related to Datagrid /////
        private bool isnewdatarow = false;
        private void gridControl1_BeginningNewRow(object sender, DataGridBeginningNewRowEventArgs e)
        {
            //MessageBox.Show("Adding new row in data grid.");
            isnewdatarow = true;
            int curRowindex = gridControl1.CurrentRow.Index;
            int colcount = ds.Variables.Count;
            string newemptyrow = CreateEmptyRowCollection(colcount);
            if (gridControl1.Rows.Count <= 3)
            {
                string s = gridControl1.CurrentCell.Text;
                IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
                analyticServ.AddNewDatagridRow("", s, newemptyrow, gridControl1.SelectedIndex, ds.Name);
                ds.RowCount++;
                //////data.Add(new object());
                ds.Changed = true;
                refreshDataGrid();
                isnewdatarow = false;
            }


           //AddNewRowWithoutEditOnCell();
        }

        object rowclassobject = null;
        string newrowdata=null;
        private void gridControl1_CommittingNewRow(object sender, DataGridEndingNewRowEventArgs e)
        {
            //MessageBox.Show("Datagrid new row commintting.");

            Type classtype = (Data as VirtualListDynamic).RowClassType;

            //storing refrence of current row class object so that is can be used later while
            //CommittedNewRow event to add new row to UI grid.
            rowclassobject = gridControl1.CurrentRow.DataItem;

            //Also create a rowdata array from above object that can be used to pass
            // to R for creating new row in R data frame also.
            PropertyInfo[] properties = classtype.GetProperties();
            int propcount = properties.Length;
            string[] strrowdata = new string[propcount];
            object colstr=null;
            try
            {
                for (int i = 0; i < propcount; i++)
                {
                    colstr = properties[i].GetValue(rowclassobject, null);
                    if (colstr != null)
                    {
                        strrowdata[i] = colstr.ToString();
                    }
                    else
                    {
                        strrowdata[i] = string.Empty;// or NA or NaN
                    }
                }
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("Error reading values from new row: ", LogLevelEnum.Error);
                logService.WriteToLogLevel(ex.Message, LogLevelEnum.Error);
            }
            //using strrowdata create something like c(4,2, "Male", 34, "Good") for passing as an argument in R call
            StringBuilder sb = new StringBuilder("c(");
            string comma = ","; // for separating diff values
            double temp;
            for (int i = 0; i < strrowdata.Length; i++)
            {
                if (i + 1 == strrowdata.Length) //for last item comma is not required.
                    comma = string.Empty;

                if (Double.TryParse(strrowdata[i], out temp))//if  its number
                {
                    sb.Append(strrowdata[i] + comma);
                }
                else//its string, put quotes around
                {
                    sb.Append("'"+strrowdata[i]+"'" + comma);
                }
            }
            sb.Append(")"); //add closing round bracket.

            if (sb.Length > 0)
            {
                newrowdata = sb.ToString();
            }
            
        }

        private void gridControl1_CommittedNewRow(object sender, C1.WPF.DataGrid.DataGridRowEventArgs e)
        {
            #region mix code. Refresh grid UI. add new row to R. Refresh grid from R.
           // //MessageBox.Show("Committed new row in data grid.");
           // //get all column values of a new row
           // //e.Row.DataItem

           // string s = gridControl1.CurrentCell.Text;
           // UAReturn result = null;
           // //22Jun2015 blanks will be converted to NAs  if (s == "") s = ".";

           // ///// Modifying R side data in Dataset ////////
           // IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();

           // //19Jun2015 Get R side varname(diff set of valid chars for defining vars) of a variable in C#
           // string RVarName = GetRVarName(gridControl1.CurrentColumn.Name);

           // //19Jun2015 following adds new row in UI. So that we don't have to reload whole dataset. At this point we can
           // // make blanks as <NA> for grid without refresh from R side
           // if (rowclassobject != null)
           // {
           //     (gridControl1.ItemsSource as IList).Add(rowclassobject);
           //     gridControl1.Reload(true);
           //     rowclassobject = null;
           // }

           // //19Jun2015 result=analyticServ.AddNewDatagridRow(e.Column.Name, s, e.Row.Index, ds.Name);
           // if (newrowdata == null)
           // {
           //     newrowdata = "c()";
           // }
           // else
           // {
           //     result = analyticServ.AddNewDatagridRow(RVarName, s, newrowdata, e.Row.Index, ds.Name);//19Jun2015
           //     newdatarow = false;

           //     //row added, so refreshgrid can bring it from R if it knows the right numer of rows in dataframe
           //     //if this is missing refreshDataGrid() below will not have any effect
           //     ds.RowCount++;
           // }

           // //23Jun2015
           // //refresh grid: this will read all the rows from R dataframe
           // // To improve performance for large dataset you can just put right values in the new row and no need to read from R.
           //refreshDataGrid();
            #endregion



            //Probably you should use any one of the follwoing two
            //===================================================//

            bool dontReloadWholeDataframeFromR = false;
            //false means I want to reload R dataframe again, after my new row gets added to R dataframe.
            //true means I refresh UI from values in C# and also send same data back to R. And no need to fresh whole grid from R
            if (dontReloadWholeDataframeFromR)
            {
                // First method
                //Does not reloads all the data again from R dataframe. So should performance should be good.
                //But UI will not show <NA> in blank cells in new row. 
                //You would need to click on "Refresh" icon. (which will reload whole dataset from R and cause performance issue for large dataset)
                //So we need to fix this so as to make the blanks as NAs in UI itself, just before adding new UI row to UI grid.
                RefreshGridWithoutReloadingFromR(e.Row.Index);
            }
            else
            {
                //Second Method
                //Adds new row to R side datafram and then reloads the grid from there. Performance issue for large datasets
                // But no need to handle <NA> in grid logic, values are being read from R which already has NAs for new empty cells of new row.
                RefreshGridFromRAfterAddingNewRowToR(e.Row.Index);
            }
        }

        //Function to update UI grid and send same data to R and there is no need to reload whole grid
        private void RefreshGridWithoutReloadingFromR(int rindex)
        {
            string s = gridControl1.CurrentCell.Text;
            UAReturn result = null;

            //22Jun2015 blanks will be converted to NAs  if (s == "") s = ".";

            ///// Modifying R side data in Dataset ////////
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();

            //19Jun2015 Get R side varname(diff set of valid chars for defining vars) of a variable in C#
            string RVarName = GetRVarName(gridControl1.CurrentColumn.Name);

            //19Jun2015 following adds new row in UI. So that we don't have to reload whole dataset. At this point we can
            // make blanks as <NA> for grid without refresh from R side
            if (rowclassobject != null)
            {
                (gridControl1.ItemsSource as IList).Add(rowclassobject); //Add new UI row with new values to UI grid
                gridControl1.Reload(true);
                rowclassobject = null;
            }

            //19Jun2015 result=analyticServ.AddNewDatagridRow(e.Column.Name, s, e.Row.Index, ds.Name);
            if (newrowdata == null)
            {
                newrowdata = "c()";
            }
            else
            {
                //Add new row to R dataframe. And no need to refresh grid as we have already added new row to UI grid above.
                result = analyticServ.AddNewDatagridRow(RVarName, s, newrowdata, rindex, ds.Name);//19Jun2015
                isnewdatarow = false;
            }
        }


        //Add row to R side and refresh dataset from R dataframe again: For large datasets it could cause performance issues
        // You can use RefreshgridWithoutReloadingFromR() in place of this function, but you need to run some test to make sure
        private void RefreshGridFromRAfterAddingNewRowToR(int rindex)
        {
            //MessageBox.Show("Committed new row in data grid.");
            string s = gridControl1.CurrentCell.Text;
            UAReturn result = null;

            //22Jun2015 blanks will be converted to NAs  if (s == "") s = ".";

            ///// Modifying R side data in Dataset ////////
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();

            //19Jun2015 Get R side varname(diff set of valid chars for defining vars) of a variable in C#
            string RVarName = GetRVarName(gridControl1.CurrentColumn.Name);

            //19Jun2015 result=analyticServ.AddNewDatagridRow(e.Column.Name, s, e.Row.Index, ds.Name);
            if (newrowdata == null)
            {
                newrowdata = "c()";
            }
            else
            {
                result = analyticServ.AddNewDatagridRow(RVarName, s, newrowdata, rindex, ds.Name);//19Jun2015
                isnewdatarow = false;

                //row added, so refreshgrid can bring it from R if it knows the right numer of rows in dataframe
                //if this is missing refreshDataGrid() below will not have any effect
                ds.RowCount++;
            }

            //23Jun2015
            //refresh grid: this will read all the rows from R dataframe
            // To improve performance for large dataset you can just put right values in the new row and no need to read from R.
            refreshDataGrid();
        }

        private void gridControl1_CommittingEdit(object sender, DataGridEndingEditEventArgs e)
        {
            string s = gridControl1.CurrentCell.Text;
            UAReturn result = null;

            if (s.Trim() == "" || s.Trim() == "<NA>") s = "";//22Jun2015 blanks will be converted to NAs  

            //MessageBox.Show("CommittingEdit new row in data grid." + e.Row.Index + ":" + e.Column.Index + ":" + e.Column.Name + ":" + s);

            ///// Modifying R side data in Dataset ////////
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();

            //19Sep2014 Get R side varname(diff set of valid chars for defining vars) of a variable in C#
            string RVarName = GetRVarName(e.Column.Name);
            int varidx = GetRVarNameIndex(RVarName);
            
            if (isnewdatarow)//Add new row
            {
                //19Sep2014 result=analyticServ.AddNewDatagridRow(e.Column.Name, s, e.Row.Index, ds.Name);
                //result = analyticServ.AddNewDatagridRow(RVarName, s, e.Row.Index, ds.Name);//19ep2014
                //isnewdatarow = false;
            }
            else //Edit existing
            {
                //Check if Dataclass(and Da taType) is right and then decide to skip this value if it does not match to the column type
                if (s.Trim().Length > 0 &&  !IsNumericStr(s) &&
                    ( ( ds.Variables[varidx].DataClass.Equals("numeric") && ds.Variables[varidx].DataType== DataColumnTypeEnum.Integer) ||
                    (ds.Variables[varidx].DataClass.Equals("numeric") && ds.Variables[varidx].DataType == DataColumnTypeEnum.Double) ||
                    (ds.Variables[varidx].DataClass.Equals("integer") && ds.Variables[varidx].DataType == DataColumnTypeEnum.Integer)
                    ))
                {
                    //Non numeric typled in numeric field
                    //Clean the cell
                    gridControl1.CancelEdit();
                    string msg = "Invalid value, you have entered a non-numeric value for a numeric variable. If you want to enter a non-numeric values make this variable a factor and enter non-numeric levels";
                    MessageBox.Show(msg, "Warning", MessageBoxButton.OK);
                    s = "";
                    return;
                }

                //19Sep2014 result=analyticServ.EditDatagridCell(e.Column.Name, s, e.Row.Index, ds.Name);
                result = analyticServ.EditDatagridCell(RVarName, s, e.Row.Index, ds.Name);//19Sep2014
            }
            ds.Changed = true;
            //refreshDataGrid();
            ///08Jul2013 Show Error/Warning in output window if any.
            //if(result!=null && result.Data != null)
            //    SendErrorWarningToOutput(result);
            
        }

        //Anil.19Sep2014
        //This method returns R side variable name for a C# side var name passed
        //This is needed because some valid chars for defining R side variables are not 
        //valid in C#. So we also store real var name (R var name) somewhere in DSV
        //And whenever needed we can pull it out
        //I have no idea how dialog will beahave, we may need to fix there something or else fix here
        private string GetRVarName(string Name)
        {
            string RVarname=Name;//set default to avoid crash etc.. This default will be overwritten
            foreach (DataSourceVariable tdsv in ds.Variables)
            {
                if(tdsv.Name.Equals(Name))//if found get RName, else default is already set.
                {
                    RVarname = tdsv.RName;
                    break;
                }
            }
            return RVarname;
        }

        //Get var name index
        private int GetRVarNameIndex(string Name)
        {
            int varidx = -1;//not found
            string RVarname = Name;//set default to avoid crash etc.. This default will be overwritten
            foreach (DataSourceVariable tdsv in ds.Variables)
            {
                varidx++;
                if (tdsv.Name.Equals(Name))//if found get RName, else default is already set.
                {
                    break;
                }
            }
            return varidx;
        }

        private int datagridrowindex;
        private void gridControl1_BeginningEdit(object sender, C1.WPF.DataGrid.DataGridBeginningEditEventArgs e)
        {
            // MessageBox.Show("Beginning Edit.."); When you click on cel to edit it and its getting ready  for edit
            datagridrowindex = gridControl1.CurrentCell.Row.Index;//gender
            rowid = gridControl1.CurrentCell.Row.DataItem.ToString();//gender
            rowindex = gridControl1.CurrentRow.Index; 
        }

        private void gridControl1_SelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
        {

            // on click gives details about the cell
            //string selectedData = "";
            //string cellValue = gridControl1.CurrentCell.Text;
            if (gridControl1.CurrentCell != null)//CurrentRow earlier
                datagridrowindex = gridControl1.CurrentCell.Row.Index;
            //string colid = gridControl1.CurrentCell.Column.Header.ToString();
            //selectedData += datagridrowindex + ":" + colid + ": " + cellValue + "\n";
            //MessageBox.Show(selectedData);

        }
        //private string datagriddelcolname;
        //private void gridControl1_DeletingRows(object sender, DataGridDeletingRowsEventArgs e)
        //{
        //    //MessageBox.Show("Dell..");
        //    datagriddelcolname = datagridrowindex; // right nowassuming single row is to be deleted
        //}

        private void gridControl1_RowsDeleted(object sender, DataGridRowsDeletedEventArgs e)
        {
            MessageBox.Show(datagridrowindex.ToString());

            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            //foreach (var idx in e.DeletedRows)
            //{
            //    datagridrowindex=idx.Index;
            //    analyticServ.RemoveDatagridRow(datagridrowindex, ds.Name);//removing R side
            //}
            analyticServ.RemoveDatagridRow(datagridrowindex, ds.Name);//removing R side

            //renumbering
            renumberRowHeader(gridControl1);
            //if (gridControl1.Viewport != null)
            //{
            //    foreach (var row in gridControl1.Viewport.Rows)
            //    {
            //        UpdateRow(row);
            //    }
            //}

            //////refreshing datagrid. I guess its already been done before executing this block(in VirtualList.cs)
            //IUnityContainer container = LifetimeService.Instance.Container;
            //IDataService service = container.Resolve<IDataService>();
            //IUIController controller = container.Resolve<IUIController>();
            //controller.RefreshDataSet(ds); //LoadNewDataSet(ds);
        }

        private void gridControl1_LoadedRowHeaderPresenter(object sender, C1.WPF.DataGrid.DataGridRowEventArgs e)
        {
            UpdateRow(e.Row);
        }

        private void gridControl1_LoadedColumnHeaderPresenter(object sender, C1.WPF.DataGrid.DataGridColumnEventArgs e)
        {
            bool runit = false;
            if(runit)
            foreach (DataSourceVariable dsv in DS.Variables)
            {
                //////////// Col header sort icon logic ///////18Apr2013
                StackPanel sp = new StackPanel(); //sp.Background = Brushes.Black;
                sp.Orientation = Orientation.Horizontal;
                sp.HorizontalAlignment = HorizontalAlignment.Left;
                sp.VerticalAlignment = VerticalAlignment.Center;
                //sp.Margin = new Thickness(1, 1, 1, 1);

                System.Windows.Controls.Label lb = new System.Windows.Controls.Label(); //lb.FontWeight = FontWeights.DemiBold;
                lb.Content = dsv.Name;
                //lb.ToolTip = ds.FileName; lb.Margin = new Thickness(1, 1, 1, 1);
                //Button b = new Button(); b.Content="x";
                Image b = new Image(); //b.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(b_MouseLeftButtonUp);
                b.ToolTip = "Close this dataset"; b.Height = 16.0; b.Width = 22.0;

                string packUri = string.Empty;
                if (dsv.Name == "accid")//dsv.SortType > 0)//ascending
                {
                    packUri = "pack://application:,,,/BlueSky;component/Images/sorted_check.png";
                }
                else if (dsv.SortType < 0)//descending
                {
                    packUri = "pack://application:,,,/BlueSky;component/Images/cut.png";
                }
                else
                {
                    packUri = "pack://application:,,,/BlueSky;component/Images/center.png";
                }
                b.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
                sp.Children.Add(lb);
                sp.Children.Add(b);
                e.Column.Header = sp;
                /////////////////////sort icon logic ends//////////////////
            }
            
        }

        private void gridControl1_AutoGeneratingColumn(object sender, C1.WPF.DataGrid.DataGridAutoGeneratingColumnEventArgs e)
        {

            e.Column.EditOnSelection = true;//one click edit mode.
            //DS.Variables // list of all vars
            //if variable is of nominal type (ordinal is also nominal), then create new List of level names and store them on
            // that col
            foreach (DataSourceVariable dsv in DS.Variables)
            {
                if ((dsv.Name == e.Property.Name) && (dsv.Measure != DataColumnMeasureEnum.Scale) && (dsv.DataType != DataColumnTypeEnum.Character))//Character condition added on 02Jul2015 So character data will be treated as Nominal in grid but in R they are not factor type.
                {

                    List<string> droplst = dsv.Values;
                    ///09Oct2013 We may not need '.'. This topic is under discussion.
                    if (!droplst.Contains("<NA>"))
                        droplst.Add("<NA>");

                    var comboCol = new C1.WPF.DataGrid.DataGridComboBoxColumn(e.Property);
                    //comboCol.DisplayMemberPath =  "Name";
                    //comboCol.SelectedValuePath = "ProductModelID";
                    comboCol.ItemsSource = droplst;
                    comboCol.EditOnSelection = true;
                   // comboCol.Name = e.Property.Name;
                    //comboCol.SelectedValuePath = droplst.ElementAt(0);
                    e.Column = comboCol;
                    e.Cancel = false; 
                }

                ////  Creating column header with sort icon  ////10Apr2014
                StackPanel colheaderpanel = new StackPanel();
                colheaderpanel.Orientation = Orientation.Horizontal;

                //putting text before sort icon
                TextBlock txb = new TextBlock();
                //19Sep2014 txb.Text = e.Property.Name;
                txb.Text = GetRVarName(e.Property.Name);//19Sep2014 support R valid chars(in C# they are invalid)
                txb.Margin = new Thickness(2);

                /// Putting Sort icon in each column header
                Image sortico = new Image(); //b.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(b_MouseLeftButtonUp);
                sortico.ToolTip = "Sorted"; sortico.Height = 16.0; sortico.Width = 22.0; sortico.Margin = new Thickness(2);
                string packUri = null;
                if(sortorder!=null && sortorder.Equals("asc"))
                    packUri = "pack://application:,,,/BlueSky;component/Images/angle-arrow-up.png";
                else if (sortorder != null && sortorder.Equals("desc"))
                    packUri = "pack://application:,,,/BlueSky;component/Images/angle-arrow-down.png";
                else
                    packUri = "pack://application:,,,/BlueSky;component/Images/sorted_check.png";//default, not needed. Null exception if not used. fix later.

                sortico.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
                
                if(sortcolnames ==null || !sortcolnames.Contains(e.Property.Name)) // dont show sort icon on other columns
                sortico.Visibility = System.Windows.Visibility.Collapsed;

                colheaderpanel.Children.Add(txb);
                colheaderpanel.Children.Add(sortico);

                e.Column.Header = colheaderpanel;
                
            }
            
            //// Defining Style for Col-Header to have sort icon ///
            //Style st = new Style(typeof(DataGridColumnHeaderPresenter));
            //st.Setters.Add(new Setter() { Property = TabItem.BackgroundProperty, Value = Brushes.DarkGray });

            //Trigger tg = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            //tg.Setters.Add(new Setter() { Property = TabItem.BackgroundProperty, Value = Brushes.Red });

            //st.Triggers.Add(tg);
            //e.Column.HeaderStyle = st;
        }


        #endregion


        #region Re-Numbering / Refreshing Grid

        private void renumberRowHeader(C1DataGrid c1grid)//for refresh row header numbers on add/delete.
        {
            ///// renumbering////////
            if (c1grid.Viewport != null)
            {
                foreach (var row in c1grid.Viewport.Rows)
                {
                    if (row.Index != -1 && row != null)
                        UpdateRow(row);
                }
            }
        }

        private static void UpdateRow(C1.WPF.DataGrid.DataGridRow row)
        {
            row.HeaderPresenter.Content = new TextBlock()
            {
                Text = (row.Index + 1).ToString(),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
        }

        private void refreshDataGrid()
        {
            ShowMouseBusy();
            //refreshing datagrid
            IUnityContainer container = LifetimeService.Instance.Container;
            IDataService service = container.Resolve<IDataService>();
            IUIController controller = container.Resolve<IUIController>();

            if (variableGrid.CurrentCell != null) //11jun2015 only 'if' added around existing code to stop[ the crash while deleting var from vargrid of new blank Dataset(or may be existing dataset say cars.sav) also.
            {
                //10May2014 If Dataset has sorted column and we try to modify Scale/Nominal/Ordinal, then sorted arrow should go away
                // Be carefull, here we are doing this only if a change is made to Scale/Nom/Ord,
                // we have no idea this is also suitable if we just do any other modifcation to data or var grid
                // For other modification, there may be a need when to remove sort icon and when not.
                string rowid = string.Empty;
                if (variableGrid.CurrentCell.Row.DataItem != null)
                {
                    rowid = variableGrid.CurrentCell.Row.DataItem.ToString();
                }
                else
                {
                    int rowidx = variableGrid.CurrentCell.Row.Index;
                    if(rowidx>=0)
                        rowid = (this.Variables[rowindex] as DataSourceVariable).RName;
                    
                }

                //Type-1
                //controller.sortcolnames.Remove(rowid);// remove sort icon from only column that was modified.

                //Type-2
                //controller.sortcolnames = null; // we need to remove sorting icon from all columns, no matter which was changed(Scal/Nominal).

                //Type-3
                // if out of all sorted cols, any one is going under Scale/Nominal change then remove sort icon from all of them. 
                // if the changed col is not one of the sorted cols then there should be no need to remove sort icon from any of them.
                if (controller.sortcolnames != null && controller.sortcolnames.Contains(rowid))
                    controller.sortcolnames = null;
            }
            ds = service.Refresh(ds);
            controller.RefreshGrids(ds);//  controller.RefreshDataSet(ds);
            //variableGrid.Refresh();
            HideMouseBusy();
        }

        public void ReloadRefreshC1Grid()//26Mar2013
        {
            gridControl1.Refresh();
        }

        #endregion

        //3Dec2013
        #region Statusbar

        public void RefreshStatusBar()
        {
            string name = DS.Name;
            //FrameworkElement fe = (string)OutputHelper.GetGlobalMacro(string.Format("GLOBAL.{0}.SPLIT", OutputHelper.AnalyticsData.DataSource.Name), ""); //DS.SplitInfo;
            string splitVars = (string)OutputHelper.GetGlobalMacro(string.Format("GLOBAL.{0}.SPLIT",name), "SelectedVars");
            if (splitVars != null)//&& splitVars.Count > 1)
            {
                name = "Split on : " + splitVars.Replace('\'',' ');//[0];
            }
            else
            {
                name = "";// "No Split";
            }
            
            statusbar.Text = name;
        }
        #endregion


        #region ContextMenu

        private void variableGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in variableGrid.Rows) 
            {
                if (item.IsMouseOver)
                {
                    variableGrid.SelectedIndex = item.Index;
                    break;
                }
            }
        }

        private void _addfactorlevel_Click(object sender, RoutedEventArgs e)
        {
            int rowindex = variableGrid.SelectedIndex;
            string colname = (this.Variables[rowindex] as DataSourceVariable).RName;
            DataColumnMeasureEnum measure = (this.Variables[rowindex] as DataSourceVariable).Measure;

            if (measure == DataColumnMeasureEnum.Scale)//dont let user run "add factor " on scale col.
            {
                MessageBox.Show("Cannot add factors to Measure=Scale(Numeric) type.", "Add/Remove Level Warning:", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<string> oldlvls = new List<string>();
            List<string> newlvls = new List<string>();
            List<string> levels = new List<string>();
            foreach (var v in ds.Variables[rowindex].Values)
            {
                levels.Add(v);
                oldlvls.Add(v);
            }
            AddFactorLevelsDialog fld = new AddFactorLevelsDialog();
            fld.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            fld.FactorLevels = levels;
            fld.ShowDialog();

            foreach (string s in fld.FactorLevels)
            {
                if (s != "<NA>" && !oldlvls.Contains(s))
                {
                    newlvls.Add(s);
                }
            }


            //Pass new levels only
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            analyticServ.AddFactorLevels(colname, newlvls, ds.Name);
            refreshDataGrid();
        }

        private void _changelabel_Click(object sender, RoutedEventArgs e)
        {
            int rowindex = variableGrid.SelectedIndex;
            //int rowid = variableGrid.CurrentRow.Index;
            int validrowidx = rowindex;//usually this is correct. But when you are on first row(index 0) then things become tricky so we need CurrentRow logic
            //if (rowindex < 0 && rowid >= 0)
            //    validrowidx = rowid;
            //else if (rowid < 0 && rowindex >= 0)
            //    validrowidx = rowindex;
            //else if (rowindex == rowid)
            //    validrowidx = rowid;

            if(validrowidx>=0)
            {
                ChangeLabels(validrowidx);
            }
        }

        private void ChangeLabels(int rowindex)
        {
            if (rowindex < 0)
            {
                //Wrong row index
                return;
            }
            //int rowindex = variableGrid.SelectedIndex;
            string colname = (this.Variables[rowindex] as DataSourceVariable).RName;
            DataColumnMeasureEnum measure = (this.Variables[rowindex] as DataSourceVariable).Measure;

            if (measure == DataColumnMeasureEnum.Scale)//dont let user run "change  levels" on scale col.
            {
                MessageBox.Show("This operation is not valid for variable of type Measure = Scale(Numeric)", "Change Level Warning:", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            List<FactorMap> fctmaplst = new List<FactorMap>();
            foreach (var v in ds.Variables[rowindex].Values)
            {
                FactorMap cpyfm = new FactorMap();
                cpyfm.labels = v;
                cpyfm.textbox = v;
                fctmaplst.Add(cpyfm);
            }
            ValueLabelsSubDialog vlsd = new ValueLabelsSubDialog(fctmaplst, "Existing Label", "New Label");
            vlsd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            vlsd.ShowDialog();
            if (!vlsd.OKclicked)
            {
                return;
            }
            List<ValLvlListItem> finalList = new List<ValLvlListItem>();
            ValLvlListItem vlit;
            foreach (FactorMap fm in fctmaplst)
            {
                if (fm.labels != "<NA>" || fm.textbox != "<NA>")
                {
                    vlit = new ValLvlListItem();
                    vlit.OriginalLevel = fm.labels;
                    vlit.NewLevel = fm.textbox;
                    finalList.Add(vlit);
                }
            }
            analyticServ.ChangeColumnLevels(colname, finalList, ds.Name);
            refreshDataGrid();
        }

        private void _makeFactor_Click(object sender, RoutedEventArgs e)
        {
            int rowindex = variableGrid.SelectedIndex;
            string colname = (this.Variables[rowindex] as DataSourceVariable).RName;
            DataColumnMeasureEnum measure = (this.Variables[rowindex] as DataSourceVariable).Measure;

            //if (measure != DataColumnMeasureEnum.Scale)//dont let user run "change  levels" on scale col.
            //{
            //    MessageBox.Show("Already a factor(Measure=Nominal/Ordinal) type", "Make Factor Warning:", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}            

            int varidx = 0;
            if (rowindex > 0)
                varidx = rowindex;
            string colid = "Measure";
            List<string> colLevels = getLevels();

             ///// Modifying R side Dataset ////////
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            if (colname != null)//new row
            {//edit existing row
                UAReturn retval = analyticServ.MakeColFactor(ds.Name, colname);
                //retval.Success = true;
                ///08Jul2013 Show Error/Warning in output window if any.
                //if (retval != null && retval.Data != null)
                //    SendErrorWarningToOutput(retval);
            }
            //variableGrid. = cellValue;
            //MessageBox.Show("[R:"+rowid + "] [C:" + colid + "] [V:" + cellValue + "] [DS:" + ds.Name+"]");
            ds.Changed = true;
            refreshDataGrid();
            //variableGrid.CurrentCell.Value = DS.Variables[rowindex].Values;
            (((variableGrid.ItemsSource) as IList)[rowindex] as DataSourceVariable).Values = DS.Variables[rowindex].Values;//Values
            (((variableGrid.ItemsSource) as IList)[rowindex] as DataSourceVariable).DataType = DS.Variables[rowindex].DataType;//DataType
            (((variableGrid.ItemsSource) as IList)[rowindex] as DataSourceVariable).DataClass = DS.Variables[rowindex].DataClass;//DataClass
            (((variableGrid.ItemsSource) as IList)[rowindex] as DataSourceVariable).Measure = DS.Variables[rowindex].Measure;//Measure
            (((variableGrid.ItemsSource) as IList)[rowindex] as DataSourceVariable).Label = DS.Variables[rowindex].Label;//Label
            variableGrid.Refresh();
            //arrangeVarGridCols();//rearrange the var-grid cols
        }

        private void _nomOrd2Scale_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _nomToOrd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _ordToNom_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _insertNewVar_Click(object sender, RoutedEventArgs e)
        {
            DataSourceVariable var = new DataSourceVariable();
            //string RecCount = (this.Variables.Count + 1).ToString();//add 1 because its 0 based
            
            string varname = "newvar";
            //getRightClickRowIndex();
            int rowindex = variableGrid.SelectedIndex;

            
            //checking duplicate var names
            do
            {
                varname = "newvar" + varcount.ToString();
                varcount++;
            } while (this.Variables.Contains(varname));
            var.Name = varname;
            var.Label = varname;

            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            analyticServ.addNewVariable(var.Name, ".", rowindex + 1, ds.Name);

            this.Variables.Insert(rowindex, var);
            DS.Variables.Insert(rowindex, var);//one more refresh needed. I guess

            renumberRowHeader(variableGrid);
            ds.Changed = true;
            refreshDataGrid(); 
         }

        private void _insertNewVarAtEnd_Click(object sender, RoutedEventArgs e)
        {
            DataSourceVariable var = new DataSourceVariable();
            //string RecCount = (this.Variables.Count + 1).ToString();//add 1 because its 0 based

            string varname = "newvar";
            //getRightClickRowIndex();
            int rowindex = Variables.Count;// variableGrid.SelectedIndex;


            //checking duplicate var names
            do
            {
                varname = "newvar" + varcount.ToString();
                varcount++;
            } while (this.Variables.Contains(varname));
            var.Name = varname;
            var.Label = varname;

            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            analyticServ.addNewVariable(var.Name, ".", rowindex + 1, ds.Name);

            this.Variables.Insert(rowindex, var);
            DS.Variables.Insert(rowindex, var);//one more refresh needed. I guess

            renumberRowHeader(variableGrid);
            ds.Changed = true;
            refreshDataGrid();
        }

        private void _deleteVar_Click(object sender, RoutedEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("Do you want to delete variable?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DataSourceVariable var = new DataSourceVariable();
                //getRightClickRowIndex();
                int rowindex = variableGrid.SelectedIndex;
                //var.Name = "accid";
                //this.Variables.Remove(var);
                variableGrid.RemoveRow(rowindex);//two things .grid remove UI dataset side remove. third is R side remove
                variableGrid.Refresh();
                // renumberRowHeader(); //not required. I guess automatically handeled by RemoveRow() above.
                ds.Changed = true;

                //this.Variables.Remove(rowindex);
                //refreshDataGrid();
            }
        }

        private void _insertNewData_Click(object sender, RoutedEventArgs e)
        {
            int colcount = ds.Variables.Count;
            string newemptyrow = CreateEmptyRowCollection(colcount); // "c('','','','','','','','','','','','','')"; //Hard coded. Create it dynamically

            string s = gridControl1.CurrentCell.Text;
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            analyticServ.AddNewDatagridRow("", s, newemptyrow, gridControl1.SelectedIndex, ds.Name);
            ds.RowCount++;
            //data.Add(new object());
            ds.Changed = true;
            refreshDataGrid();
        }

        //returns something like c('','','','') for colcount 4 and likewise depending on colcount
        private string CreateEmptyRowCollection(int colcount)
        {
            StringBuilder emptyrow = new StringBuilder("c(");
            for(int i=0; i < colcount; i++)
            {
                if (i + 1 == colcount)//if we are on last col
                {
                    emptyrow.Append("'')");
                }
                else
                {
                    emptyrow.Append("'',");
                }
            }
            return emptyrow.ToString();
 
        }

        private void _deleteData_Click(object sender, RoutedEventArgs e)
        {
            IAnalyticsService analyticServ = LifetimeService.Instance.Container.Resolve<IAnalyticsService>();
            analyticServ.RemoveDatagridRow(gridControl1.SelectedIndex, ds.Name);//removing R side

            ds.RowCount--;
            //renumbering
            renumberRowHeader(gridControl1);
            ds.Changed = true;
            refreshDataGrid();
        }

        void datagridContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            c1gridContextMenuOpening(sender, e, gridControl1);
        }

        private void variableGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            c1gridContextMenuOpening(sender, e, variableGrid);
        }

        private void c1gridContextMenuOpening(object sender, ContextMenuEventArgs e, C1DataGrid gridname)
        {
            DependencyObject dobj = (DependencyObject)e.OriginalSource;
            dobj = VisualTreeHelper.GetParent(dobj);
            dobj = VisualTreeHelper.GetParent(dobj);
            dobj = VisualTreeHelper.GetParent(dobj);
            if (dobj.DependencyObjectType.Name.Equals("DataGridRowHeaderPresenter") || dobj.DependencyObjectType.Name.Equals("DataGridRowsHeaderPanel"))
            {
                e.Handled = false;
                foreach (var item in gridname.Rows)
                {
                    if (item.IsMouseOver)
                    {
                        gridname.SelectedIndex = item.Index;
                        break;
                    }
                }
            }
            else
            {
                e.Handled = true;
                return;
            }
        }

        #endregion


        #region Show errors/warning in output window

        private void SendErrorWarningToOutput(UAReturn retval)//08jul2013
        {
            OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
            OutputWindow ow = owc.ActiveOutputWindow as OutputWindow; //get currently active window
            IUIController UIController = LifetimeService.Instance.Container.Resolve<IUIController>();

            OutputHelper.Reset();
            OutputHelper.UpdateMacro("%DATASET%", UIController.GetActiveDocument().Name);
            retval.Success = true;
            AnalyticsData data = new AnalyticsData();
            //data.SelectedForDump = selectedForDump;//10Jan2013
            //data.PreparedCommand = cmd.CommandSyntax;//storing command
            data.Result = retval;
            data.AnalysisType = retval.CommandString; //"T-Test"; For Parent Node name 02Aug2012
            //data.InputElement = element;
            //data.DataSource = ds;
            //data.OutputTemplate = ((UAMenuCommand)parameter).commandoutputformat;
            UIController.AnalysisComplete(data);
            //ow.AddAnalyis(data);

            //08Apr2015 bring main window in front after file open, instead of output window
            Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();
            window.Activate();
        }

        #endregion

        private void variableGrid_CancelingNewRow(object sender, DataGridEndingNewRowEventArgs e)
        {

        }

        #region Mouse Busy/Free and Keyboard events

        Cursor defaultcursor;
        private void ShowMouseBusy()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }
        private void HideMouseBusy()
        {
            Mouse.OverrideCursor = null;// defaultcursor;
        }

        // Showing a busy message while var grid is loading 08Aug2013
        private void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.OverrideCursor == System.Windows.Input.Cursors.Wait)
                e.Handled = true;
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Mouse.OverrideCursor == System.Windows.Input.Cursors.Wait)
                e.Handled = true;
        }

        //Which tab is clicked Data OR Variables
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Mouse.OverrideCursor == System.Windows.Input.Cursors.Wait)
            {
                e.Handled = true;
            }
            else
            {
                string tabHeader = ((sender as TabControl).SelectedItem as TabItem).Header as string;

                switch (tabHeader)
                {
                    case "Data":

                        break;

                    case "Variables":
                        ShowMouseBusy();
                        break;
                    default:
                        return;
                }
            }
        }


        #endregion




    }

    #region Converters
    public class AlignConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.Parse(typeof(DataColumnAlignmentEnum), value.ToString());
        }
    }

    public class MeasureConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.Parse(typeof(DataColumnMeasureEnum), value.ToString());
        }
    }

    public class RoleConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.Parse(typeof(DataColumnRole), value.ToString());
        }
    }

    public class ComboImageSourceConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            //BitmapImage bitmap = new BitmapImage(new Uri(@"C:\w2s.png"));//new BitmapImage(new Uri((parameter as string) + (string)value, UriKind.RelativeOrAbsolute));
            //return bitmap;
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            if (value == null)
            {
                image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/left.png");//new Uri(@"C:\w2s.png");
            }
            else
            {

                switch (value.ToString())
                {
                    case "Left":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/Left.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Center":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/center.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Right":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/right.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Nominal":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/nominal.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Ordinal":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/ordinal.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Scale":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/scale.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Input":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/input.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Target":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/target.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Both":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/both.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "None":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/none.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Partition":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/partition.png");//new Uri(@"C:\w2s.png");
                        break;
                    case "Split":
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/split.png");//new Uri(@"C:\w2s.png");
                        break;
                    default:
                        image.UriSource = new Uri(@"pack://application:,,,/BlueSky;component/Images/imagenotfound.png");//new Uri(@"C:\w2s.png");
                        break;

                }
            }
            image.EndInit();

            return image;
            //return Properties.Resources.Save;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

    public class ValueLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string returnStr = string.Empty;
            if (value != null)
            {
                List<string> lst = (List<string>)value;
                string[] vals = lst.ToArray();
                for (int i = 0; i < vals.Length; i++)
                {
                    if (vals[i] != null && vals[i].Trim().Length == 0) // to avoid balnk values from getting in {}
                        continue;
                    returnStr = returnStr + ("{");//("{" + i + "-");
                    if (i + 1 == vals.Length)
                        returnStr = returnStr + vals[i] + "}";
                    else
                        returnStr = returnStr + vals[i] + "}..."; //returnStr = returnStr + vals[i] + "}, ";

                    break;//just to have single value. To get all remove break and uncomment code in 'else'
                }
                return (string)returnStr;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            DateTime resultDateTime;
            if (DateTime.TryParse(strValue, out resultDateTime))
            {
                return resultDateTime;
            }
            return DependencyProperty.UnsetValue;
        }
    }

    public class MissingValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string returnStr = string.Empty;
            if (value != null)
            {
                List<string> lst = (List<string>)value;
                string[] vals = lst.ToArray();
                for (int i = 0; i < vals.Length; i++)
                {
                    returnStr = returnStr + ("{");//("{" + i + "-");
                    if (i + 1 == vals.Length)
                        returnStr = returnStr + vals[i] + "}";
                    else
                        returnStr = returnStr + vals[i] + "}..."; //returnStr = returnStr + vals[i] + "}, ";
                    break;//just to have single value. To get all remove break and uncomment code in 'else'
                }
                return (string)returnStr;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            DateTime resultDateTime;
            if (DateTime.TryParse(strValue, out resultDateTime))
            {
                return resultDateTime;
            }
            return DependencyProperty.UnsetValue;
        }
    }

    public class DataRowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            C1.WPF.DataGrid.DataGridRow row = value as C1.WPF.DataGrid.DataGridRow;
            if (row != null)
                return row.Index;
            else
                return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return DependencyProperty.UnsetValue;
        }
    }

    public class DataGridFactorConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.Parse(typeof(string), value.ToString());
        }
    }

    #endregion

    #region var grid columns
    public class DataGridValueLablesCol : C1.WPF.DataGrid.DataGridColumn
    {

        // Height of each level
        public static int LevelHeaderHeight = 18;

        // Inner Columns
        public ObservableCollection<C1.WPF.DataGrid.DataGridColumn> InnerColumns { get; set; }

        // Global Header
        public object CompositeHeader { get; set; }

        // Nested Levels
        public int NestedLevels { get; private set; }

        public DataGridValueLablesCol()
        {
            InnerColumns = new ObservableCollection<C1.WPF.DataGrid.DataGridColumn>();

            // the following features are not implemented
            CanUserResize = false;
            CanUserSort = false;
            CanUserFilter = false;
            IsReadOnly = true; //edit on textfield is not allowed
        }

        public void Update()
        {
            double totalWidth = 0;
            int maxNestedLevels = 0;

            // initialize grid
            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(DataGridValueLablesCol.LevelHeaderHeight) });
            panel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(DataGridValueLablesCol.LevelHeaderHeight) });
            foreach (var col in InnerColumns)
            {
                // support nested scenarios
                var cc = col as DataGridValueLablesCol;
                if (cc != null)
                {
                    cc.Update();
                    maxNestedLevels = Math.Max(cc.NestedLevels, maxNestedLevels);
                }

                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
                totalWidth += col.Width.Value;
            }

            // add global header
            var globalHeader = new ContentControl() { Content = CompositeHeader };
            Grid.SetColumnSpan(globalHeader, InnerColumns.Count);
            panel.Children.Add(globalHeader);

            // add individual headers
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];

                // add nested headers
                var content = new DataGridColumnHeaderPresenter() { Content = col.Header, Background = new SolidColorBrush(Colors.Transparent) };
                Grid.SetColumn(content, i);
                Grid.SetRow(content, 1);
                panel.Children.Add(content);
            }

            // update header & global width
            Header = panel;
            Width = new C1.WPF.DataGrid.DataGridLength(totalWidth);
            NestedLevels = 1 + maxNestedLevels;
        }

        #region Cell Content

        public override object GetCellContentRecyclingKey(C1.WPF.DataGrid.DataGridRow row)
        {
            return typeof(DataGridValueLablesCol);
        }

        public override FrameworkElement CreateCellContent(C1.WPF.DataGrid.DataGridRow row)
        {
            // initialize grid
            var panel = new Grid();
            foreach (var col in InnerColumns)
            {
                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
            }

            // add individual content
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var content = col.CreateCellContent(row);
                Grid.SetColumn(content, i);
                panel.Children.Add(content);
            }
            return panel;
        }

        public override void BindCellContent(FrameworkElement cellContent, C1.WPF.DataGrid.DataGridRow row)
        {
            Panel panel = (Panel)cellContent;

            // bind individual cells
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var control = panel.Children[i] as FrameworkElement;
                col.BindCellContent(control, row);
            }
        }

        public override void UnbindCellContent(FrameworkElement cellContent, C1.WPF.DataGrid.DataGridRow row)
        {
            Panel panel = (Panel)cellContent;

            // bind individual cells
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var control = panel.Children[i] as FrameworkElement;
                col.UnbindCellContent(control, row);
            }
        }

        #endregion

        #region Editing

        public override FrameworkElement GetCellEditingContent(C1.WPF.DataGrid.DataGridRow row)
        {
            // initialize grid
            var panel = new Grid();
            foreach (var col in InnerColumns)
            {
                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
            }

            //add individual content
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var content = col.GetCellEditingContent(row);
                Grid.SetColumn(content, i);
                panel.Children.Add(content);
            }
            return panel;
        }

        public override object PrepareCellForEdit(FrameworkElement editingElement)
        {
            // compose all the values into a list of objects
            List<object> values = new List<object>();
            var children = (editingElement as Panel).Children;

            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var value = InnerColumns[i].PrepareCellForEdit(children[i] as FrameworkElement);
                values.Add(value);
            }
            return values;
        }

        public override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            // decompose all the values from the list of objects
            // and invoke each of the cancels
            var values = (List<object>)uneditedValue;
            var children = (editingElement as Panel).Children;

            for (int i = 0; i < InnerColumns.Count; i++)
            {
                InnerColumns[i].CancelCellEdit(children[i] as FrameworkElement, values[i]);
            }
        }

        public override bool BeginEdit(FrameworkElement editingElement, RoutedEventArgs routedEventArgs)
        {
            // pass input to first column
            if (InnerColumns.Count > 0)
            {
                var children = (editingElement as Panel).Children;
                return InnerColumns[0].BeginEdit(children[0] as Control, routedEventArgs);
            }
            return false;
        }

        public override void EndEdit(FrameworkElement editingElement)
        {
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                if (editingElement is Panel)
                {
                    var children = (editingElement as Panel).Children;
                    if (children.Count > i)
                    {
                        InnerColumns[i].EndEdit(children[i] as Control);
                    }
                }
            }
        }

        #endregion
    }

    class DataGridMissingCol : C1.WPF.DataGrid.DataGridColumn
    {
        // Height of each level
        public static int LevelHeaderHeight = 18;

        // Inner Columns
        public ObservableCollection<C1.WPF.DataGrid.DataGridColumn> InnerColumns { get; set; }

        // Global Header
        public object CompositeHeader { get; set; }

        // Nested Levels
        public int NestedLevels { get; private set; }

        public DataGridMissingCol()
        {
            InnerColumns = new ObservableCollection<C1.WPF.DataGrid.DataGridColumn>();

            // the following features are not implemented
            CanUserResize = false;
            CanUserSort = false;
            CanUserFilter = false;
            IsReadOnly = true;//fix for bug. Stop edit on text field
        }

        public void Update()
        {
            double totalWidth = 0;
            int maxNestedLevels = 0;

            // initialize grid
            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(DataGridValueLablesCol.LevelHeaderHeight) });
            panel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(DataGridValueLablesCol.LevelHeaderHeight) });
            foreach (var col in InnerColumns)
            {
                // support nested scenarios
                var cc = col as DataGridValueLablesCol;
                if (cc != null)
                {
                    cc.Update();
                    maxNestedLevels = Math.Max(cc.NestedLevels, maxNestedLevels);
                }

                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
                totalWidth += col.Width.Value;
            }

            // add global header
            var globalHeader = new ContentControl() { Content = CompositeHeader };
            Grid.SetColumnSpan(globalHeader, InnerColumns.Count);
            panel.Children.Add(globalHeader);

            // add individual headers
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];

                // add nested headers
                var content = new DataGridColumnHeaderPresenter() { Content = col.Header, Background = new SolidColorBrush(Colors.Transparent) };
                Grid.SetColumn(content, i);
                Grid.SetRow(content, 1);
                panel.Children.Add(content);
            }

            // update header & global width
            Header = panel;
            Width = new C1.WPF.DataGrid.DataGridLength(totalWidth);
            NestedLevels = 1 + maxNestedLevels;
        }

        #region Cell Content

        public override object GetCellContentRecyclingKey(C1.WPF.DataGrid.DataGridRow row)
        {
            return typeof(DataGridMissingCol);
        }

        public override FrameworkElement CreateCellContent(C1.WPF.DataGrid.DataGridRow row)
        {
            // initialize grid
            var panel = new Grid();
            foreach (var col in InnerColumns)
            {
                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
            }

            // add individual content
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var content = col.CreateCellContent(row);
                Grid.SetColumn(content, i);
                panel.Children.Add(content);
            }
            return panel;
        }

        public override void BindCellContent(FrameworkElement cellContent, C1.WPF.DataGrid.DataGridRow row)
        {
            Panel panel = (Panel)cellContent;

            // bind individual cells
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var control = panel.Children[i] as FrameworkElement;
                col.BindCellContent(control, row);
            }
        }

        public override void UnbindCellContent(FrameworkElement cellContent, C1.WPF.DataGrid.DataGridRow row)
        {
            Panel panel = (Panel)cellContent;

            // bind individual cells
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var control = panel.Children[i] as FrameworkElement;
                col.UnbindCellContent(control, row);
            }
        }

        #endregion

        #region Editing

        public override FrameworkElement GetCellEditingContent(C1.WPF.DataGrid.DataGridRow row)
        {
            // initialize grid
            var panel = new Grid();
            foreach (var col in InnerColumns)
            {
                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
            }

            // add individual content
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var content = col.GetCellEditingContent(row);//////bug for Missing
                Grid.SetColumn(content, i);
                panel.Children.Add(content);
            }
            return panel;
        }

        public override object PrepareCellForEdit(FrameworkElement editingElement)
        {
            // compose all the values into a list of objects
            List<object> values = new List<object>();
            var children = (editingElement as Panel).Children;

            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var value = InnerColumns[i].PrepareCellForEdit(children[i] as FrameworkElement);
                values.Add(value);
            }
            return values;
        }

        public override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            //decompose all the values from the list of objects
            //and invoke each of the cancels
            var values = (List<object>)uneditedValue;
            var children = (editingElement as Panel).Children;

            for (int i = 0; i < InnerColumns.Count; i++)
            {
                InnerColumns[i].CancelCellEdit(children[i] as FrameworkElement, values[i]);
            }
        }

        public override bool BeginEdit(FrameworkElement editingElement, RoutedEventArgs routedEventArgs)
        {
            // pass input to first column
            if (InnerColumns.Count > 0)
            {
                var children = (editingElement as Panel).Children;
                return InnerColumns[0].BeginEdit(children[0] as Control, routedEventArgs);
            }
            return false;
        }

        public override void EndEdit(FrameworkElement editingElement)
        {
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                if (editingElement is Panel)
                {
                    var children = (editingElement as Panel).Children;
                    if (children.Count > i)
                    {
                        InnerColumns[i].EndEdit(children[i] as Control);
                    }
                }
            }
        }

        #endregion
    }

    class DataGridAlignCol : C1.WPF.DataGrid.DataGridColumn
    {
        // Height of each level
        public static int LevelHeaderHeight = 18;

        // Inner Columns
        public ObservableCollection<C1.WPF.DataGrid.DataGridColumn> InnerColumns { get; set; }

        // Global Header
        public object CompositeHeader { get; set; }

        // Nested Levels
        public int NestedLevels { get; private set; }

        public DataGridAlignCol()
        {
            InnerColumns = new ObservableCollection<C1.WPF.DataGrid.DataGridColumn>();

            // the following features are not implemented
            CanUserResize = false;
            CanUserSort = false;
            CanUserFilter = false;
        }

        public void Update()
        {
            double totalWidth = 0;
            int maxNestedLevels = 0;

            // initialize grid
            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(DataGridValueLablesCol.LevelHeaderHeight) });
            panel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(DataGridValueLablesCol.LevelHeaderHeight) });
            foreach (var col in InnerColumns)
            {
                // support nested scenarios
                var cc = col as DataGridValueLablesCol;
                if (cc != null)
                {
                    cc.Update();
                    maxNestedLevels = Math.Max(cc.NestedLevels, maxNestedLevels);
                }

                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
                totalWidth += col.Width.Value;
            }

            // add global header
            var globalHeader = new ContentControl() { Content = CompositeHeader };
            Grid.SetColumnSpan(globalHeader, InnerColumns.Count);
            panel.Children.Add(globalHeader);

            // add individual headers
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];

                // add nested headers
                var content = new DataGridColumnHeaderPresenter() { Content = col.Header, Background = new SolidColorBrush(Colors.Transparent) };
                Grid.SetColumn(content, i);
                Grid.SetRow(content, 1);
                panel.Children.Add(content);
            }

            // update header & global width
            Header = panel;
            Width = new C1.WPF.DataGrid.DataGridLength(totalWidth);
            NestedLevels = 1 + maxNestedLevels;
        }

        #region Cell Content

        public override object GetCellContentRecyclingKey(C1.WPF.DataGrid.DataGridRow row)
        {
            return typeof(DataGridValueLablesCol);
        }

        public override FrameworkElement CreateCellContent(C1.WPF.DataGrid.DataGridRow row)
        {
            // initialize grid
            var panel = new Grid();
            foreach (var col in InnerColumns)
            {
                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
            }

            // add individual content
            for (int i = 0; i < InnerColumns.Count; i++)
            {
                var col = InnerColumns[i];
                var content = col.CreateCellContent(row);
                //e.Column = new C1.WPF.DataGrid.DataGridComboBoxColumn(e.Property);
                Grid.SetColumn(content, i);
                panel.Children.Add(content);
            }
            return panel;
        }

        public override void BindCellContent(FrameworkElement cellContent, C1.WPF.DataGrid.DataGridRow row)
        {
            Panel panel = (Panel)cellContent;

            // bind individual cells
            //for (int i = 0; i < InnerColumns.Count; i++)
            //{
            //    var col = InnerColumns[i];
            //    var control = panel.Children[i] as FrameworkElement;
            //    col.BindCellContent(control, row);
            //}
        }

        public override void UnbindCellContent(FrameworkElement cellContent, C1.WPF.DataGrid.DataGridRow row)
        {
            Panel panel = (Panel)cellContent;

            //// bind individual cells
            //for (int i = 0; i < InnerColumns.Count; i++)
            //{
            //    var col = InnerColumns[i];
            //    var control = panel.Children[i] as FrameworkElement;
            //    col.UnbindCellContent(control, row);
            //}
        }

        #endregion

        #region Editing

        public override FrameworkElement GetCellEditingContent(C1.WPF.DataGrid.DataGridRow row)
        {
            // initialize grid
            var panel = new Grid();
            foreach (var col in InnerColumns)
            {
                panel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col.Width.Value) });
            }

            // add individual content
            //    for (int i = 0; i < InnerColumns.Count; i++)
            //    {
            //        var col = InnerColumns[i];
            //        var content = col.GetCellEditingContent(row);
            //        Grid.SetColumn(content, i);
            //        panel.Children.Add(content);
            //    }
            return panel;
        }

        public override object PrepareCellForEdit(FrameworkElement editingElement)
        {
            // compose all the values into a list of objects
            List<object> values = new List<object>();
            var children = (editingElement as Panel).Children;

            //for (int i = 0; i < InnerColumns.Count; i++)
            //{
            //    var value = InnerColumns[i].PrepareCellForEdit(children[i] as FrameworkElement);
            //    values.Add(value);
            //}
            return values;
        }

        public override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            // decompose all the values from the list of objects
            // and invoke each of the cancels
            //var values = (List<object>)uneditedValue;
            //var children = (editingElement as Panel).Children;

            //for (int i = 0; i < InnerColumns.Count; i++)
            //{
            //    InnerColumns[i].CancelCellEdit(children[i] as FrameworkElement, values[i]);
            //}
        }

        public override bool BeginEdit(FrameworkElement editingElement, RoutedEventArgs routedEventArgs)
        {
            // pass input to first column
            //if (InnerColumns.Count > 0)
            //{
            //    var children = (editingElement as Panel).Children;
            //    return InnerColumns[0].BeginEdit(children[0] as Control, routedEventArgs);
            //}
            return false;
        }

        public override void EndEdit(FrameworkElement editingElement)
        {
            //for (int i = 0; i < InnerColumns.Count; i++)
            //{
            //    if (editingElement is Panel)
            //    {
            //        var children = (editingElement as Panel).Children;
            //        if (children.Count > i)
            //        {
            //            InnerColumns[i].EndEdit(children[i] as Control);
            //        }
            //    }
            //}
        }

        #endregion
    }
    #endregion
}
