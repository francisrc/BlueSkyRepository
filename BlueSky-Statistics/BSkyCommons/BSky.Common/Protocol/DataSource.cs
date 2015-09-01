using System.Collections.Generic;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using System;

namespace BSky.Statistics.Common
{
    
    public enum UADataType : uint { UAUnKnown, UAInt, UAIntList, UAIntMatrix, UAString, UAStringList, UAStringMatrix, UADouble, UADoubleList, UADoubleMatrix, UAList, UATableList, UADataFrame }
    public enum DataColumnTypeEnum : uint { Integer, Numeric, Double, Factor, Ordinal, Character, Logical, POSIXlt, POSIXct, Date, Unknown }
    public enum DataColumnAlignmentEnum : uint { Left, Right, Center }
    public enum DataColumnMeasureEnum : uint { Scale, Ordinal, Nominal }
    public enum DataColumnRole : uint { Input, Target, Both, None, Partition, Split }

    public class DataSource
    {
        //for getting maxfactor from config options settings
        IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//03Feb2015

        private object data;

        public object Data
        {
            get { return data; }
            set { data = value; }
        }
        private bool hasHeader;

        public bool HasHeader
        {
            get { return hasHeader; }
            set { hasHeader = value; }
        }
        private string fieldSperator;

        public string FieldSperator
        {
            get { return fieldSperator; }
            set { fieldSperator = value; }
        }
        private string extension;


        public string Extension
        {
            get { return extension; }
            set { extension = value; }
        }
        private bool isLoaded;


        public bool IsLoaded
        {
            get { return isLoaded; }
            set { isLoaded = value; }
        }
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private string fileName;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        private string sheetName; //29Apr2015 Added so that, multiple sheets from same Excel file can be opened in Datagrid tabs

        public string SheetName
        {
            get { return sheetName; }
            set { sheetName = value; }
        }

        private int rowCount;

        public int RowCount
        {
            get { return rowCount; }
            set { rowCount = value; }
        }

        //15Apr2014. For reloading dataset, we need to know filetype. This is best place to do it.
        public string FileType 
        {
            get
            {
                string extn = extension.ToLower();
                string ftype = string.Empty;
                if (extn.Equals("sav")) ftype = "SPSS";
                else if (extn.Equals("xls")) ftype = "XLS";
                else if (extn.Equals("xlsx")) ftype = "XLSX";
                else if (extn.Equals("csv")) ftype = "CSV";
                else if (extn.Equals("dbf")) ftype = "DBF";
                else if (extn.Equals("rdata")) ftype = "RDATA";
                else if (extn.Equals("txt")) ftype = "TXT";

                return ftype;
            }
        }
        private List<DataSourceVariable> variables = new List<DataSourceVariable>();

        public List<DataSourceVariable> Variables
        {
            get { return variables; }
            set { variables = value; }
        }

        //03Dec2013 Stausbar Split info:
        //public string SplitInfo
        //{
        //    get;
        //    set;
        //}

        //private ObservableCollection<DataSourceVariable> _ObservableVariables = new ObservableCollection<DataSourceVariable>();
        //public ObservableCollection<DataSourceVariable> ObservableVariables
        //{
        //    get { return _ObservableVariables; }
        //    set { _ObservableVariables = ObservableVariables; }
        //}


        private bool changed;// Added by Anil to track if dataset is changed. Confirmation popup appears while closing dataset.

        public bool Changed
        {
            get { return changed; }
            set { changed = value; }
        }

        private int _maxfactor;// Maximum factors allowed.
        public int maxfactor
        {
            get
            {
                //get latest maxfactor from config settings, if available.
                string maxfac = confService.GetConfigValueForKey("maxfactorcount");
                if (maxfac.Trim().Length != 0)
                {
                    Int32.TryParse(maxfac, out _maxfactor);
                }
                //setting maximum limit. Later we can figure out what is the limits for R or Rcmdr
                // and the set accordingly
                if (_maxfactor > 100)
                    _maxfactor = 100;

                return _maxfactor;
            }
            set
            {
                _maxfactor = value;
            }
        }

        //for storing error/warning or info message to pass it to different layer
        // WE can also have Enum to set if message is for ERROR, WARNING or INFO
        private string message;
        public string Message 
        {
            get { return message; }
            set { message = value; } 
        }

    }
    public class DataSourceVariable
    {

        
        
        
        
        
        private string _Name;




        public string Name
        {
            get { return _Name; }
            set
            {
                _RName = value;//19Sep2014
                _Name = _RName.Replace(".", "_").Replace("(", "_").Replace(")", "_"); //19Jul2015 putting back replace. //value;//Looks like the issue fixed by component1 and we do not need to do this -> .Replace(".", "");//19Sep2014
                _XName = value;
            }
        }



        private string _XName;




        public string XName
        {
            get { return _XName; }
            set
            {
                _XName = value;//19Sep2014
                //_Name = _RName.Replace(".", "_"); //19Jul2015 putting back replace. //value;//Looks like the issue fixed by component1 and we do not need to do this -> .Replace(".", "");//19Sep2014
            }
        }






        private DataColumnTypeEnum _dataType = DataColumnTypeEnum.Numeric; //typeof of the col

        public DataColumnTypeEnum DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
        }

        private string _dataClass = ""; //class of the column

        public string DataClass
        {
            get { return _dataClass; }
            set { _dataClass = value; }
        }

        private int _width = 4;

        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        private int _decimals = 0;

        public int Decimals
        {
            get { return _decimals; }
            set { _decimals = value; }
        }

        private string _Label;

        public string Label
        {
            get { return _Label; }
            set { _Label = value; }
        }

        private List<string> _Values = new List<string>();

        public List<string> Values
        {
            get { return _Values; }
            set { _Values = value; }
        }

        private List<string> _Missing = new List<string>();

        public List<string> Missing
        {
            get { return _Missing; }
            set { _Missing = value; }
        }

        private string _missingType;//none, three, range+1

        public string MissType
        {
            get { return _missingType; }
            set { _missingType = value; }
        }

        private uint _Columns = 8;//changed from object type to uint. Always +ve int.  Anil

        public uint Columns
        {
            get { return _Columns; }
            set { _Columns = value; }
        }

        private DataColumnAlignmentEnum _Alignment = DataColumnAlignmentEnum.Left;

        public DataColumnAlignmentEnum Alignment
        {
            get { return _Alignment; }
            set { _Alignment = value; }
        }
        private DataColumnMeasureEnum _Measure = DataColumnMeasureEnum.Scale;

        public DataColumnMeasureEnum Measure
        {
            get { return _Measure; }
            set
            {
                _Measure = value; /// if following path does not work then use converters ///
                if (_Measure == DataColumnMeasureEnum.Nominal) _ImgURL = "/Images/nominal.png";
                else if (_Measure == DataColumnMeasureEnum.Ordinal) _ImgURL = "/Images/ordinal.png";
                else if (_Measure == DataColumnMeasureEnum.Scale) _ImgURL = "/Images/scale.png";
                else _ImgURL = "/Images/none.png";
            }
        }
        private DataColumnRole _Role = DataColumnRole.Input;

        public DataColumnRole Role
        {
            get { return _Role; }
            set { _Role = value; }
        }

        private int _RowCount;// remove this property. Anil

        public int RowCount
        {
            get { return _RowCount; }
            set { _RowCount = value; }
        }

        public override string ToString()
        {
            //30Sep2014 return this.Name;
            return this.RName; //30Sep2014
        }

        private string _ImgURL;
        public string ImgURL
        {
            get { return _ImgURL; }
            set { _ImgURL = value; }
        }

        //17Apr2014 Map was getting lost earlier and must be saved with each column, if exists(if Scale to Nominal/Ordinal is done)
        private List<FactorMap> _factormapList = new List<FactorMap>();
        public List<FactorMap> factormapList
        {
            get
            {
                return _factormapList;
            }
            set
            {
                _factormapList = factormapList;
            }
        }

        //18Apr2013//
        private int _sortType;
        public int SortType
        {
            get { return _sortType; }
            set
            {
                if (value < 0)
                    _sortType = -1;//Descending
                else if (value > 0)
                    _sortType = 1; //Ascending
                else
                    _sortType = 0; // No Sorting
            }
        }

        //19Sep2014 For real var name that can have invalid(c#) naming character( not invalid for R)
        private string _RName;
        public string RName
        {
            get { return _RName; }
            //set { _RName = value; } //no need 'Name' prop will set the value.
        }
    }

    //Added by Aaron 08/26/2014
    //Added by Aaron to capture the name and icon for a dataset
    //We are going to use the same code for variable lists and datasets. So we create a stack panel in the listbox to
    //display the ico and text(name) of the dataset

    public class DatasetDisplay
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _imgurl;
        public string ImgURL
        {
            get { return _imgurl; }
            set { _imgurl =value; }
        }

    }

}
