using System;
using System.Linq;
using BSky.Lifetime.Interfaces;
using BSky.Lifetime;
using BSky.Statistics.Common;
using RDotNet;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualBasic.CompilerServices;
using System.Collections;
using System.IO;

namespace BSky.Statistics.R
{
    public class RService
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012
        IConfigService confService = LifetimeService.Instance.Container.Resolve<IConfigService>();//23nov2012
        bool AdvancedLogging;
        string tableheader = null;
        public class RCommandReturn
        {
            public const string Success = "no error";
        }

        RDotNetConsoleLogDevice _log;//for logging

        public Exception LastException { get; set; }

        private string _lastCommand = string.Empty;

        private REngine _RServer = null;
        private DataFrame _DF = null;

        #region Ctor...



        public RService()
            : base()
        {
            string RLogFilename = DirectoryHelper.GetLogFileName();
            this._log = new RDotNetConsoleLogDevice();
            this._log.LogDevice = new Journal() { FileName = RLogFilename };
            logService.WriteToLogLevel("R DotNet Server (deepest function call) initialization started.", LogLevelEnum.Info); 
            try
            {

                StartupParameter sp = new StartupParameter(); 
                REngine.SetEnvironmentVariables(null, null );//can set R Path and R Home by passing params
                this._RServer = REngine.GetInstance();//null, true, null, _log);
                this._RServer.Initialize();
                logService.WriteToLogLevel("R DotNet Server initialized.", LogLevelEnum.Info); //writes to ApplicationLog
                _log.WriteConsole("R.Net Initialized!!!", 1024, RDotNet.Internals.ConsoleOutputType.None); //writes to RLog
            }
            catch (Exception ex)
            {
                _log.WriteConsole("Unable to initialize R Server.(note: 64bit R must already be present)", 5, RDotNet.Internals.ConsoleOutputType.None);//Added by Anil
                logService.WriteToLogLevel("Unable to initialize R Server.", LogLevelEnum.Error, ex);
                
                throw new Exception();
            }
            logService.WriteToLogLevel("R  DotNet Server (deepest function call) initialization ended.", LogLevelEnum.Info); 
        }

        public void Close()
        {
            logService.WriteToLogLevel("Closing R R.Net Server...", LogLevelEnum.Info);
            this._RServer.Close();
        }

        public DataFrame GetDataFrame(string dsname)
        {
            if (dsname == null)
                return null;
            DataFrame _df = _RServer.Evaluate(dsname).AsDataFrame();//Dataset1, Dataset2 etc..
            return _df;
        }

        #endregion

        #region Graphics Support
        public void AddGraphicsDevice(string DeviceName, object Device)
        {
            //this._RServer.AddGraphicsDevice(DeviceName, (ISGFX)Device);
        }
        #endregion

        #region XML
        public XmlDocument ParseToXmlDocument(string objectName)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode rootNode = doc.CreateElement("Root");
            doc.AppendChild(rootNode);
            
            ParseToXmlNode(rootNode, objectName);
            //Fill UASUMMARY to existing DOM
            ParseUASummary(rootNode, objectName);
            return doc; ///XML DOM object created for several R command output
        }

        private void ParseObjectToXmlNode(XmlNode parent, object objectName)
        {

        }

        public void ParseToXmlNode(XmlNode parent, string objectName)
        {
            XmlNode thisNode;
            if (!(bool)this._RServer.Evaluate("is.null(" + objectName + ")").AsLogical()[0])//"is.null( tmp )"
            {
                object data = null;
                object executionStat = null; //19Jun2013 for holding the exeution status from return structure
                object returnval = null; //20Jun2013 for holding result table value.
                string typeName = string.Empty;
                string classtype = string.Empty;
                try
                {
                    string cmd = "is.na(" + objectName + ")";
                    LogicalVector tms = this._RServer.Evaluate(cmd).AsLogical();

                    if (tms == null || tms.Length == 0)//no data to process
                    {
                        return;
                    }
                    if (tms.Length==1)//single elements !tms.GetType().IsArray)
                    {
                        if (tms[0]) //A. to see if NA coming 
                        {
                            UADataType dataType = getUADataTypeFromName("String");
                            thisNode = parent.OwnerDocument.CreateElement(getElementTypeName(dataType));
                            parent.AppendChild(thisNode);
                            thisNode.InnerText = ".";
                            return;
                        }
                        else
                        {
                            object b = this._RServer.Evaluate("class(" + objectName + ")").AsCharacter()[0];
                            if (true)//factor
                                data = this._RServer.Evaluate("as.character(" + objectName + ")").AsCharacter()[0];//this lines read factor values ToString numeric
                            else
                                data = this._RServer.Evaluate(objectName);//this lines read factor values ToString numeric
                            if (b != null && b.ToString().ToLower().Equals("matrix"))
                                typeName = "String[,]";//"Double[,]";
                            else
                                typeName = data.GetType().Name;
                            //Console.WriteLine(b);
                        }
                    }
                    else //For those commands that return multiple tables like BSky return structure. (mainly for analysis command. But not sure.)
                    {
                        classtype = this._RServer.Evaluate("class(" + objectName + ")").AsCharacter()[0];
                        data = this._RServer.Evaluate(objectName).AsList();

                        if (classtype.Equals("data.frame") && !objectName.Contains("metadatatable"))
                        {
                            typeName = "DataFrame";
                            data = this._RServer.Evaluate(objectName).AsCharacter();
                        }
                        else if (classtype.Trim().Equals("matrix"))//"character"
                        {
                            typeName = "String[,]";
                            if (typeName.Equals("String[,]"))
                            {
                                data = this._RServer.Evaluate(objectName).AsCharacterMatrix();
                            }
                            else if (typeName.Equals("String[]"))
                            {
                                data = this._RServer.Evaluate(objectName).AsCharacter();
                            }
                        }
                        else if (classtype.Equals("table") && !objectName.Contains("metadatatable"))
                        {
                            classtype = this._RServer.Evaluate("typeof(" + objectName + ")").AsCharacter()[0];
                            if (classtype.Trim().Equals("double")) //20Sep2013 for 2wayfreq
                            {
                                typeName = "Double[,]";
                                data = this._RServer.Evaluate(objectName).AsNumericMatrix();
                            }
                            else if (classtype.Trim().Equals("matrix"))//"character"
                            {
                                typeName = "String[,]";
                                if(typeName.Equals("String[,]"))
                                {
                                    data = this._RServer.Evaluate(objectName).AsCharacterMatrix();
                                }
                                else if (typeName.Equals("String[]"))
                                {
                                    data = this._RServer.Evaluate(objectName).AsCharacter();
                                }
                            }
                            else
                            {
                                typeName = "Int32[,]";//OR "matrix";
                                data = this._RServer.Evaluate(objectName).AsIntegerMatrix();
                            }
                        }
                        else if (classtype.Equals("data.frame") && objectName.Contains("metadatatable"))
                        {
                            typeName = "Object[]";
                            data = new string[8];
                            //data = this._RServer.Evaluate(objectName).AsList();
                        }
                        else if (classtype.Equals("list") && (data as GenericVector).Length == 13) //bad if condition here. Improve it later. HArd coded for col props
                        {
                            typeName = "String[]";
                            data = this._RServer.Evaluate(objectName).AsCharacter();
                        }
                        else if (classtype.Equals("character") || classtype.Equals("numeric")) //bad if condition here. Improve it later. HArd coded for col props
                        {
                            typeName = "String[]";
                            data = this._RServer.Evaluate(objectName).AsCharacter();
                        }
                        else
                        {
                            typeName = data.GetType().Name;
                            //data = this._RServer.Evaluate(objectName).AsList();
                            if (typeName.Equals("GenericVector"))
                                classtype = "list";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ///data = this._RServer.Evaluate(objectName); //for 2d Matrix
                    if (data != null)
                        typeName = "Table[]";
                }
                ///////////Fix for One Smpl 13Feb2012 ///////
                if (classtype.Equals("list") && parent.Name.Equals("Root"))
                {
                    try
                    {
                        data = this._RServer.Evaluate(string.Format("{0}[[7]]", objectName)).AsCharacter()[0];
                        if (data != null && data.ToString().Length > 0)//str len check for graphic objectName[[7]]
                            typeName = "Table[]";
                    }
                    catch (Exception ee)
                    {
                        //data = this._RServer.Evaluate(objectName); //no need of this statement I guess
                        logService.WriteToLogLevel(ee.Message, LogLevelEnum.Error);
                    }
                }
                //////////////13Feb2012///////
                if (null != data)
                {
                    UADataType dataType = getUADataTypeFromName(typeName);//datatype will b diff for each datatype. Can be made generic. "DataType"
                    thisNode = parent.OwnerDocument.CreateElement(getElementTypeName(dataType));
                    parent.AppendChild(thisNode);

                    switch (typeName)
                    {
                        case "String":
                            thisNode.InnerText = (string)data;
                            break;

                        case "String[]":
                            string[] sList = (data as CharacterVector).ToArray();
                            foreach (String r in sList)
                            {
                                XmlNode row = thisNode.OwnerDocument.CreateElement("row");
                                row.InnerText = r;
                                thisNode.AppendChild(row);
                            }
                            returnVal=sList;//added for R.NET
                            break;

                        case "Double":
                            if (((double)data).ToString() == "-2146826246")
                                thisNode.InnerText = "NA";
                            else
                                thisNode.InnerText = ((double)data).ToString();
                            break;
                        case "Double[]":
                            double[] dList = (double[])data;
                            foreach (double r in dList)
                            {
                                XmlNode row = thisNode.OwnerDocument.CreateElement("row");
                                if (r.ToString() == "-2146826246")
                                    row.InnerText = "NA";
                                else
                                    row.InnerText = r.ToString();
                                thisNode.AppendChild(row);
                            }
                            break;
                        case "Double[,]":
                            GenerateSlicenameAndRowColHeaders(objectName, thisNode);

                            // Creating DOM using matrix data
                            NumericMatrix tempNM = (data as NumericMatrix);
                            long rowCount = tempNM!=null ? tempNM.RowCount:1;
                            long colCount = tempNM!=null ? tempNM.ColumnCount:1;
                            double[,] dMatrix = new double[rowCount, colCount];;//String[,] dMatrix = new string[rowCount, colCount];
                            for (int mi = 0; mi < rowCount; mi++)
                            {
                                for (int mj = 0; mj < colCount; mj++)
                                {
                                    dMatrix[mi, mj] = tempNM != null ? tempNM[mi, mj] : Double.Parse(data.ToString());
                                }
                            }

                            XmlNode rows = thisNode.OwnerDocument.CreateElement("rows");
                            thisNode.AppendChild(rows);
                            for (long rIndex = 0; rIndex < rowCount; rIndex++)
                            {
                                XmlNode row = rows.OwnerDocument.CreateElement("row");
                                rows.AppendChild(row);
                                XmlNode columns = row.OwnerDocument.CreateElement("columns");
                                row.AppendChild(columns);
                                for (long cIndex = 0; cIndex < colCount; cIndex++)
                                {
                                    XmlNode col = columns.OwnerDocument.CreateElement("column");
                                    columns.AppendChild(col);
                                    if (dMatrix[rIndex, cIndex].ToString() == "-2146826246")
                                    {
                                        col.InnerText = "NA";
                                        continue;
                                    }
                                    double val = 0;
                                    double.TryParse(dMatrix[rIndex, cIndex].ToString(), out val);
                                    col.InnerText = val.ToString();
                                }
                            }
                            break;

                        case "String[,]":
                            CreateTableRowColumnHeaders(objectName, thisNode);

                            CharacterMatrix tempCM = (data as CharacterMatrix);//tempCM will be null when there is 1 row 1 col(1 cell only)
                            int mtxrcount = tempCM!=null ? tempCM.RowCount:1;//multi or single cell
                            int mtxccount = tempCM!=null ? tempCM.ColumnCount:1; //multi or single cell
                            String[,] sMatrix = new string[mtxrcount, mtxccount];
                            for (int mi = 0; mi < mtxrcount; mi++)
                            {
                                for (int mj = 0; mj < mtxccount; mj++)
                                {
                                    sMatrix[mi,mj] = tempCM!=null ? tempCM[mi, mj]:(String)data;
                                }
                            }
                            ////////////


                            rowCount = sMatrix.GetLongLength(0);
                            colCount = sMatrix.GetLongLength(1);
                            rows = thisNode.OwnerDocument.CreateElement("rows");
                            thisNode.AppendChild(rows);
                            for (long rIndex = 0; rIndex < rowCount; rIndex++)
                            {
                                XmlNode row = rows.OwnerDocument.CreateElement("row");
                                rows.AppendChild(row);
                                XmlNode columns = row.OwnerDocument.CreateElement("columns");
                                row.AppendChild(columns);
                                for (long cIndex = 0; cIndex < colCount; cIndex++)
                                {
                                    XmlNode col = columns.OwnerDocument.CreateElement("column");
                                    columns.AppendChild(col);
                                    if (sMatrix[rIndex, cIndex].ToString() == "-2146826246")
                                    {
                                        col.InnerText = "NA";
                                        continue;
                                    }
                                    col.InnerText = sMatrix[rIndex, cIndex].ToString();
                                }
                            }
                            break;
                        case "Object[,]":

                            GenerateSlicenameAndRowColHeaders(objectName, thisNode);

                            object[,] oMatrix = (object[,])data;
                            rowCount = oMatrix.GetLongLength(0);
                            colCount = oMatrix.GetLongLength(1);
                            rows = thisNode.OwnerDocument.CreateElement("rows");
                            thisNode.AppendChild(rows);
                            for (long rIndex = 0; rIndex < rowCount; rIndex++)
                            {
                                XmlNode row = rows.OwnerDocument.CreateElement("row");
                                rows.AppendChild(row);
                                XmlNode columns = row.OwnerDocument.CreateElement("columns");
                                row.AppendChild(columns);
                                for (long cIndex = 0; cIndex < colCount; cIndex++)
                                {
                                    XmlNode col = columns.OwnerDocument.CreateElement("column");
                                    columns.AppendChild(col);
                                    if (oMatrix[rIndex, cIndex].ToString() == "-2146826246")
                                    {
                                        col.InnerText = "NA";
                                        continue;
                                    }
                                    col.InnerText = oMatrix[rIndex, cIndex].ToString();
                                }
                            }
                            break;

                        case "Int16":
                        case "Int32":
                        case "Int64":
                            if (((int)data).ToString() == "-2146826246")
                                thisNode.InnerText = "NA";
                            else
                                thisNode.InnerText = ((int)data).ToString();
                            break;

                        case "Int16[]":
                        case "Int32[]":
                        case "Int64[]":
                            int[] iList = (int[])data;
                            try
                            {
                                CreateTableRowColumnHeaders(objectName, thisNode);
                                //filling data in DOM
                                rows = thisNode.OwnerDocument.CreateElement("rows");
                                thisNode.AppendChild(rows);

                                foreach (double r in iList)
                                {
                                    XmlNode row = rows.OwnerDocument.CreateElement("row");
                                    if (r.ToString() == "-2146826246")
                                    {
                                        row.InnerText = "NA";
                                        continue;
                                    }
                                    row.InnerText = r.ToString();
                                    rows.AppendChild(row);
                                }
                                break;
                            }
                            catch { }

                            //filling data in DOM
                            ///rows = thisNode.OwnerDocument.CreateElement("rows");
                            ///thisNode.AppendChild(rows);
                            //int[] iList = (int[])data;
                            foreach (double r in iList)
                            {
                                XmlNode row = thisNode.OwnerDocument.CreateElement("row");
                                if (r.ToString() == "-2146826246")
                                {
                                    row.InnerText = "NA";
                                    continue;
                                }
                                row.InnerText = r.ToString();
                                thisNode.AppendChild(row);
                                ///rows.AppendChild(row);
                            }
                            break;

                        case "Int16[,]":
                        case "Int32[,]":
                        case "Int64[,]":
                            CreateTableRowColumnHeaders(objectName, thisNode, classtype);

                            int[,] iMatrix;
                            //for single row R 'table'. Single Dim array. But we need tp convert it to 2D to print headers for the 1D 'table'
                            string datatype = data.GetType().Name;
                            if (datatype.Equals("Int16[]") || datatype.Equals("Int32[]") || datatype.Equals("Int64[]"))
                            {
                                int[] tmparr = (int[])data;
                                int colsize = tmparr.Length;
                                iMatrix = new int[1, colsize];
                                int i = 0;
                                foreach (int v in tmparr)
                                {
                                    iMatrix[0, i] = tmparr[i];
                                    i++;
                                }
                            }
                            else
                            {
                                iMatrix = (int[,])data;
                            }
                            ////// Creating DOM using matrix data
                            //28Aug2013. moved above in if else ...  int[,] iMatrix = (int[,])data;
                            rowCount = iMatrix.GetLongLength(0);
                            colCount = iMatrix.GetLongLength(1);
                            rows = thisNode.OwnerDocument.CreateElement("rows");
                            thisNode.AppendChild(rows);
                            for (long rIndex = 0; rIndex < rowCount; rIndex++)
                            {
                                XmlNode row = rows.OwnerDocument.CreateElement("row");
                                rows.AppendChild(row);
                                XmlNode columns = row.OwnerDocument.CreateElement("columns");
                                row.AppendChild(columns);
                                for (long cIndex = 0; cIndex < colCount; cIndex++)
                                {
                                    XmlNode col = columns.OwnerDocument.CreateElement("column");
                                    columns.AppendChild(col);
                                    if (iMatrix[rIndex, cIndex].ToString() == "-2146826246")
                                    {
                                        col.InnerText = "NA";
                                        continue;
                                    }
                                    col.InnerText = iMatrix[rIndex, cIndex].ToString();
                                }
                            }
                            break;

                        case "Object[]":
                            {
                                Object[] oList = (Object[])data;

                                int len = oList.Length;

                                for (int index = 1; index <= len; index++) //for test make len=8
                                {
                                    //ParseToXmlNode(thisNode, objectName + "[[" + index.ToString() + "]]"); this commented & following if/else added
                                    if (index == 3 || index == 7 || index == 8)
                                        ParseToXmlNode(thisNode, "as.character(" + objectName + "[[" + index.ToString() + "]])");
                                    else
                                        ParseToXmlNode(thisNode, objectName + "[[" + index.ToString() + "]]");
                                }

                                break;
                            }
                        case "Table[]":
                            {
                                data = this._RServer.Evaluate(string.Format("{0}[[7]]", objectName)).AsCharacter()[0];
                                int numberOfTables=0;
                                if (Int32.TryParse(data.ToString(), out numberOfTables))//try converting to number.
                                { }

                                int objectcountintable = 0;
                                bool isewtable = false;//before ewtable we have BSky stat table and user tables added by BSkyBuildReturnTableStructure
                                // and have XML output template. Even if template is missing a basic formatting will be done.
                                //after ewtable we have other user tables for which XML output template is not present.
                                for (int i = 1; i <= numberOfTables; ++i)
                                {
                                    tableheader = null;
                                    string tabletype = "resulttable";
                                    /// We can also find tabletype as following (this is now implemented on 21sep2013)
                                    /// Set a flag to one state. assume all the tables those come before type="ewtable" are analytic tables.
                                    /// As soon as type="ewtable" is found, process it and set a flag that will tell that all the other tables
                                    /// are user result tables.

                                    ///// finding the table type  //////
                                    //if table[[i]]$anyname exists. For result table it should not exists
                                    bool tablepropsnamesexists = this._RServer.Evaluate(string.Format("!is.null(names({0}$tables[[{1}]]))", objectName, i)).AsLogical()[0];
                                    objectcountintable = this._RServer.Evaluate(string.Format("length({0}$tables[[{1}]])", objectName, i)).AsInteger()[0];
                                    if (this._RServer.Evaluate(string.Format("!is.null(names({0}$tables)[[{1}]])", objectName, i)).AsLogical()[0])//01May2014
                                    {
                                        tableheader = this._RServer.Evaluate(string.Format("names({0}$tables)[[{1}]]", objectName, i)).AsCharacter()[0];
                                    }
                                    if (objectcountintable > 1 && tablepropsnamesexists)
                                    {

                                        bool hasType = false;
                                        bool isatomic = this._RServer.Evaluate(string.Format("is.atomic({0}$tables[[{1}]])", objectName, i)).AsLogical()[0];
                                        if (!isatomic)
                                            hasType = this._RServer.Evaluate(string.Format("!is.null({0}$tables[[{1}]]$type)", objectName, i)).AsLogical()[0];

                                        //23Aug2013. this line replaced by abv 4 lines
                                        //bool hasType = this._RServer.Evaluate(string.Format("!is.null({0}$tables[[{1}]]$type)", objectName,i));
                                        if (hasType)
                                            tabletype = this._RServer.Evaluate(string.Format("{0}$tables[[{1}]]$type", objectName, i)).AsCharacter()[0];

                                        //find $columnNames 30apr2015 List of cols to remove( or keep)
                                        bool hasColNames = this._RServer.Evaluate(string.Format("!is.null({0}$tables[[{1}]]$columnNames)", objectName, i)).AsLogical()[0];
                                        if (hasColNames)
                                        {
                                            XmlElement colnames = parent.OwnerDocument.CreateElement("ColNames");//);[0]+metatableId.ToString()// each metadatatable will hv diff name in DOM
                                            colnames.SetAttribute("tablenumber", i.ToString());
                                            ParseToXmlNode(colnames, string.Format("{0}$tables[[{1}]]$columnNames", objectName, i));
                                            thisNode.AppendChild(colnames);
                                        }

                                    }
                                    ///Now tabletype is one of "table", "ewtable", "resulttable"

                                    if (tabletype.Equals("table") || tabletype.Equals("ewtable")) // Analytic table or error warning table
                                    {
                                        isewtable = true;//21sep2013
                                        ParseToXmlNode(thisNode, string.Format("{0}$tables[[{1}]]$datatable", objectName, i));

                                        dynamic res = this._RServer.Evaluate(string.Format("{0}$tables[[{1}]]$metadata", objectName, i)).AsCharacter()[0];///tmp[[8]][[1]]$metadata
                                        string isMetadata = res;//(string)res; 
                                        if (isMetadata == "yes")
                                        {
                                            int noMetadata = int.Parse(this._RServer.Evaluate(string.Format("{0}$tables[[{1}]]$nometadatatables", objectName, i)).AsInteger()[0].ToString());
                                            string[] Metadatanames = null;
                                            if (noMetadata == 1)
                                            {
                                                Metadatanames = new string[1];
                                                Metadatanames[0] = (string)this._RServer.Evaluate(string.Format("{0}$tables[[{1}]]$metadatatabletype", objectName, i)).AsCharacter()[0];
                                            }
                                            else
                                            {
                                                string str = (string)this._RServer.Evaluate(string.Format("{0}$tables[[{1}]]$metadatatabletype", objectName, i)).AsCharacter()[0];
                                                Metadatanames = new string[noMetadata];
                                                for (int j = 0; j < noMetadata; j++)
                                                    Metadatanames[j] = str + (j + 1).ToString();//right now we are assigning same name + index to each matadatatable related to single datatable

                                            }

                                            XmlElement metadatanodes = null;
                                            if (tabletype.Equals("table"))
                                                metadatanodes = parent.OwnerDocument.CreateElement("Metadata");//for analytic
                                            else if (tabletype.Equals("ewtable"))
                                                metadatanodes = parent.OwnerDocument.CreateElement("BSkyErrorWarn");//for error warning

                                            metadatanodes.SetAttribute("tablenumber", i.ToString());
                                            for (int metatableId = 1; metatableId <= noMetadata; ++metatableId)
                                            {
                                                XmlElement metanode = parent.OwnerDocument.CreateElement(Metadatanames[metatableId - 1]);//);[0]+metatableId.ToString()// each metadatatable will hv diff name in DOM
                                                ParseToXmlNode(metanode, string.Format("{0}$tables[[{1}]]$metadatatable[[{2}]]", objectName, i, metatableId));
                                                metadatanodes.AppendChild(metanode);
                                            }
                                            thisNode.AppendChild(metadatanodes);
                                        }
                                    }//tabletype table or ewtable

                                    else 
                                    {
                                        // this line puts the direct result. For internal commands from RCommandsString file.
                                        //This line should execute only once. Just after rwtable following retun values will be found.
                                        // We can comment this line and use the DOM thats be being created below to get user resuls.
                                        // Following line is not for users result as its mostly required for internal functions.
                                        //This returnVal value is consumed internally in app functioning rather than pushing it out to output window.
                                        //object r2 = this._RServer.Evaluate(string.Format("names({0}$tables)",objectName));
                                        //if (r2 == null) { }
                                        returnVal = this._RServer.Evaluate(string.Format("{0}$tables[[{1}]]", objectName, i));

                                        if (!isewtable)//21sep2013 For tables added using BSkyBuildReturnTableStructure. Added before "ewtable" in ret struct
                                        {
                                            ParseToXmlNode(thisNode, string.Format("{0}$tables[[{1}]]", objectName, i));
                                            continue;//move to next table
                                        }


                                        ////Putting User Results in DOM //class(ostt$tables[[6]]) => character, data.frame etc..
                                        int totalusertables = numberOfTables - i + 1;
                                        XmlElement userresult = null;
                                        userresult = parent.OwnerDocument.CreateElement("UserResult");//for analytic

                                        for (int tno = 1; tno <= totalusertables; ++tno, i++)
                                        {
                                            //Following will check if ewtable exists in between user tables ( this happens when user tables are stacked )
                                            tablepropsnamesexists = this._RServer.Evaluate(string.Format("!is.null(names({0}$tables[[{1}]]))", objectName, i)).AsLogical()[0];
                                            objectcountintable = this._RServer.Evaluate(string.Format("length({0}$tables[[{1}]])", objectName, i)).AsInteger()[0];

                                            if (objectcountintable > 1 && tablepropsnamesexists)
                                            {
                                                bool hasType = false;
                                                bool isatomic = this._RServer.Evaluate(string.Format("is.atomic({0}$tables[[{1}]])", objectName, i)).AsLogical()[0];
                                                if (!isatomic)
                                                    hasType = this._RServer.Evaluate(string.Format("!is.null({0}$tables[[{1}]]$type)", objectName, i)).AsLogical()[0];

                                                if (hasType)
                                                {
                                                    tabletype = this._RServer.Evaluate(string.Format("{0}$tables[[{1}]]$type", objectName, i)).AsCharacter()[0];
                                                    if (tabletype.Equals("ewtable"))
                                                    {
                                                        i--; // external for loop should increment it again.
                                                        break;
                                                    }
                                                }
                                            }

                                            ////// Processing one of the user tables ////
                                            XmlElement udata = parent.OwnerDocument.CreateElement("UserData");//);[0]+metatableId.ToString()// each metadatatable will hv diff name in DOM
                                            udata.SetAttribute("tablenumber", tno.ToString());
                                            ParseToXmlNode(udata, string.Format("{0}$tables[[{1}]]", objectName, i));
                                            userresult.AppendChild(udata);
                                        }
                                        thisNode.AppendChild(userresult);
                                    }
                                }// for loop on tables

                            }
                            break;
                        case "DataFrame":
                            {


                                rowCount = this._RServer.Evaluate("nrow(" + objectName + ")").AsInteger()[0];
                                colCount = this._RServer.Evaluate("ncol(" + objectName + ")").AsInteger()[0];
                                String[,] dfMatrix = new string[rowCount, colCount];
                                for (int jj = 0; jj < colCount; jj++)
                                {
                                    string[] coldata = null;


                                    int colscount = this._RServer.Evaluate("length(" + objectName + "[," + (jj + 1) + "])").AsInteger()[0];
                                    if (colscount > 1)
                                    {
                                        CharacterVector cv = this._RServer.Evaluate("as.character(" + objectName + "[," + (jj + 1) + "])").AsCharacter();
                                        int siz = cv.Count();
                                        coldata = new string[siz];
                                        for (int ic = 0; ic < siz; ic++)
                                        {
                                            coldata[ic] = cv[ic];
                                        }

                                    }
                                    else if (colscount == 1)
                                    {
                                        string coname = this._RServer.Evaluate("as.character(" + objectName + "[," + (jj + 1) + "])").AsCharacter()[0];
                                        coldata = new string[1];
                                        coldata[0] = coname;
                                    }
                                    else
                                    {
                                        //no need already set to null
                                        break;
                                    }

                                    for (int ii = 0; ii < coldata.Length; ii++)
                                    {
                                        dfMatrix[ii, jj] = coldata[ii];
                                    }
                                }

                                //Creating DOM for row col data, using array from above
                                rows = thisNode.OwnerDocument.CreateElement("rows");
                                thisNode.AppendChild(rows);
                                for (long rIndex = 0; rIndex < rowCount; rIndex++)
                                {
                                    XmlNode row = rows.OwnerDocument.CreateElement("row");
                                    rows.AppendChild(row);
                                    XmlNode columns = row.OwnerDocument.CreateElement("columns");
                                    row.AppendChild(columns);
                                    for (long cIndex = 0; cIndex < colCount; cIndex++)
                                    {
                                        XmlNode col = columns.OwnerDocument.CreateElement("column");
                                        columns.AppendChild(col);
                                        if (dfMatrix[rIndex, cIndex].ToString() == "-2146826246")
                                        {
                                            col.InnerText = "NA";
                                            continue;
                                        }
                                        col.InnerText = dfMatrix[rIndex, cIndex].ToString();
                                    }
                                }

                            }
                            break;
                    }//switch
                }
            }
        }

        //29Aug 2013 For generating extra tags that will hold row/col headers and slice name. 
        // Info in these tags can be used for displaying tables in output those do not have output format template XML
        private void GenerateSlicenameAndRowColHeaders(string objectName, XmlNode thisNode)
        {
            //Creating row col headers if any present on R side object
            string[] objcolheaders = null;
            string[] objrowheaders = null;
            string objslicetitlecommand = string.Empty;
            string objslicetitle = string.Empty;
            //finding slice name, is exists
            if (objectName.Contains("$datatable")) // only for return structures those contain $datatable (BSky stat results )
            {
                objslicetitlecommand = objectName.Replace("$datatable", "$cartlevel");
                if (!this._RServer.Evaluate("is.null(" + objslicetitlecommand + ")").AsLogical()[0])
                    objslicetitle = this._RServer.Evaluate(objslicetitlecommand).AsCharacter()[0];
                XmlElement sliceTitletag = thisNode.OwnerDocument.CreateElement("slicename");
                sliceTitletag.InnerText = objslicetitle.Replace("<", "&lt;").Replace(">", "&gt;").Replace("<=", "&le;").Replace(">=", "&ge;");
                thisNode.AppendChild(sliceTitletag);

            }
            if (!this._RServer.Evaluate("is.null(colnames(" + objectName + "))").AsLogical()[0])
            {
                CharacterVector cv = this._RServer.Evaluate("colnames(" + objectName + ")").AsCharacter();
                int siz = cv.Count();
                objcolheaders = new string[siz];
                for (int ic = 0; ic < siz; ic++)
                {
                    objcolheaders[ic] = cv[ic];
                }
            }
            if (!this._RServer.Evaluate("is.null(rownames(" + objectName + "))").AsLogical()[0])
            {
                CharacterVector cv = this._RServer.Evaluate("rownames(" + objectName + ")").AsCharacter();
                int siz = cv.Count();
                objrowheaders = new string[siz];
                for (int ic = 0; ic < siz; ic++)
                {
                    objrowheaders[ic] = cv[ic];
                }
            }

            //01May2014 table header
            XmlElement strtableheader = thisNode.OwnerDocument.CreateElement("tableheader");
            if (tableheader != null && tableheader.Length > 0)
                strtableheader.InnerText = tableheader;//Table header assigned
            thisNode.AppendChild(strtableheader);

            XmlElement objcolnames = thisNode.OwnerDocument.CreateElement("colheaders");
            if (objcolheaders != null)
            {
                string innertxt = string.Join(",", objcolheaders);//Array to comma separated string
                objcolnames.InnerText = innertxt.Replace("<", "&lt;").Replace(">", "&gt;").Replace("<=", "&le;").Replace(">=", "&ge;");
            }
            XmlElement objrownames = thisNode.OwnerDocument.CreateElement("rowheaders");
            if (objrowheaders != null)
            {
                string innertxt = string.Join(",", objrowheaders);//Array to comma separated string
                objrownames.InnerText = innertxt.Replace("<", "&lt;").Replace(">", "&gt;").Replace("<=", "&le;").Replace(">=", "&ge;");
            }
            thisNode.AppendChild(objcolnames);
            thisNode.AppendChild(objrownames);
        }

        private void CreateTableRowColumnHeadersOld(string objectName, XmlNode thisNode, string classtype = "")
        {
            //Creating row col headers if any present on R side object. 
            string[] strcolheaders = null;
            string[] strrowheaders = null;
            int srdim = 1, scdim = 1;//array with at least 1 row 1 col

            if (!this._RServer.Evaluate("is.na(ncol(" + objectName + "))").AsLogical()[0]) // if no. of col does exists 
            {
                scdim = this._RServer.Evaluate("ncol(" + objectName + ")").AsInteger()[0];
                if (!this._RServer.Evaluate("is.null(colnames(" + objectName + "))").AsLogical()[0])
                {
                    CharacterVector cv = this._RServer.Evaluate("colnames(" + objectName + ")").AsCharacter();
                    int siz = cv.Count();
                    strcolheaders = new string[siz];
                    for (int ic = 0; ic < siz; ic++)
                    {
                        strcolheaders[ic] = cv[ic];
                    }
                }
            }
            if (!this._RServer.Evaluate("is.na(nrow(" + objectName + "))").AsLogical()[0]) // if no. of row does exists 
            {
                srdim = this._RServer.Evaluate("nrow(" + objectName + ")").AsInteger()[0];
                if (!this._RServer.Evaluate("is.null(rownames(" + objectName + "))").AsLogical()[0])
                {
                    CharacterVector cv = this._RServer.Evaluate("rownames(" + objectName + ")").AsCharacter();
                    int siz = cv.Count();
                    strrowheaders = new string[siz];
                    for (int ic = 0; ic < siz; ic++)
                    {
                        strrowheaders[ic] = cv[ic];
                    }
                }
            }

            //this section for int16[,], in32[,], int64[,]. Check if this is needed for these Ints and other types too.
            // This happens with R 'table' having single row ( and multi col )
            // When in R console you print R 'table' column names are shown. But when you try to find out colnames(table)
            // its subscript out of bounds. Instead, rownames(table) gives the column name. Which is weird.
            if (srdim > 1 && scdim == 1 && classtype.Equals("table")) //changing row headers to col headers.Awkward but 1D 'table' need this. 
            {
                strcolheaders = strrowheaders;
                strrowheaders = null;
            }

            //01May2014 table header
            XmlElement strtableheader = thisNode.OwnerDocument.CreateElement("tableheader");
            if (tableheader != null && tableheader.Length > 0)
                strtableheader.InnerText = tableheader;//Table header assigned
            thisNode.AppendChild(strtableheader);


            //Col Row Headers
            XmlElement strcolnames = thisNode.OwnerDocument.CreateElement("colheaders");
            if (strcolheaders != null)
                strcolnames.InnerText = string.Join(",", strcolheaders);//Array to comma separated string
            XmlElement strrownames = thisNode.OwnerDocument.CreateElement("rowheaders");
            if (strrowheaders != null)
                strrownames.InnerText = string.Join(",", strrowheaders);//Array to comma separated string
            thisNode.AppendChild(strcolnames);
            thisNode.AppendChild(strrownames);
        }

        private void CreateTableRowColumnHeaders(string objectName, XmlNode thisNode, string classtype = "")
        {
            //Creating row col headers if any present on R side object. 
            string[] strcolheaders = null;
            string[] strrowheaders = null;
            int srdim = 1, scdim = 1;//array with at least 1 row 1 col

            if (!this._RServer.Evaluate("is.na(ncol(" + objectName + "))").AsLogical()[0]) // if no. of col does exists 
            {
                scdim = this._RServer.Evaluate("ncol(" + objectName + ")").AsInteger()[0];
                if (!this._RServer.Evaluate("is.null(colnames(" + objectName + "))").AsLogical()[0])
                {
                    CharacterVector cv = this._RServer.Evaluate("colnames(" + objectName + ")").AsCharacter();
                    int siz = cv.Count();
                    strcolheaders = new string[siz];
                    for (int ic = 0; ic < siz; ic++)
                    {
                        strcolheaders[ic] = cv[ic];
                    }
                }
            }
            if (!this._RServer.Evaluate("is.na(nrow(" + objectName + "))").AsLogical()[0]) // if no. of row does exists 
            {
                srdim = this._RServer.Evaluate("nrow(" + objectName + ")").AsInteger()[0];
                if (!this._RServer.Evaluate("is.null(rownames(" + objectName + "))").AsLogical()[0])
                {
                    CharacterVector cv = this._RServer.Evaluate("rownames(" + objectName + ")").AsCharacter();
                    int siz = cv.Count();
                    strrowheaders = new string[siz];
                    for (int ic = 0; ic < siz; ic++)
                    {
                        strrowheaders[ic] = cv[ic];
                    }
                }
            }

            //this section for int16[,], in32[,], int64[,]. Check if this is needed for these Ints and other types too.
            // This happens with R 'table' having single row ( and multi col )
            // When in R console you print R 'table' column names are shown. But when you try to find out colnames(table)
            // its subscript out of bounds. Instead, rownames(table) gives the column name. Which is weird.
            if (srdim > 1 && scdim == 1 && classtype.Equals("table")) //changing row headers to col headers.Awkward but 1D 'table' need this. 
            {
                strcolheaders = strrowheaders;
                strrowheaders = null;
            }

            //01May2014 table header
            XmlElement strtableheader = thisNode.OwnerDocument.CreateElement("tableheader");
            if (tableheader != null && tableheader.Length > 0)
                strtableheader.InnerText = tableheader;//Table header assigned
            thisNode.AppendChild(strtableheader);


            //Col Row Headers
            XmlElement strcolnames = thisNode.OwnerDocument.CreateElement("colheaders");
            if (strcolheaders != null)
                strcolnames.InnerText = string.Join(",", strcolheaders);//Array to comma separated string
            XmlElement strrownames = thisNode.OwnerDocument.CreateElement("rowheaders");
            if (strrowheaders != null)
                strrownames.InnerText = string.Join(",", strrowheaders);//Array to comma separated string
            thisNode.AppendChild(strcolnames);
            thisNode.AppendChild(strrownames);
        }

        public void ParseUASummary(XmlNode parent, string objectName)
        {
            object data = null;
            if (!(bool)this._RServer.Evaluate("is.null(" + objectName + ")").AsLogical()[0])//"is.null( tmp )"
            {
                try
                {
                    if (int.Parse(this._RServer.Evaluate("length(" + objectName.Trim() + ")").AsInteger()[0].ToString()) >= 6)
                    {
                        if (!(bool)this._RServer.Evaluate("is.na(" + "names(" + objectName.Trim() + "[6])" + ")").AsLogical()[0])
                        {
                            if (this._RServer.Evaluate("names(" + objectName.Trim() + "[6])").AsCharacter()[0].ToString() == "uasummary")
                            {
                                data = this._RServer.Evaluate("length(" + objectName.Trim() + "[[6]])").AsInteger()[0];
                                int notesize = int.Parse(data.ToString());
                                //create node for UASummary
                                XmlElement xe_uas = parent.OwnerDocument.CreateElement("UASummary");
                                XmlElement xe_ual = parent.OwnerDocument.CreateElement("UAList");
                                XmlElement xe_str = null;
                                string innrtxt = string.Empty;
                                data = this._RServer.Evaluate(string.Format("{0}[[6]]", objectName)).AsCharacter().ToArray();///uasummary [[6]]
                                if (data.GetType().IsArray)
                                {
                                    object[] newarr = (object[])data;
                                    for (int i = 0; i < notesize; ++i)
                                    {
                                        //data = this._RServer.Evaluate(string.Format("{0}[[6]][[{1}]]", objectName, i));///uasummary [[6]]

                                        //NULL=-2146826288   NA= -2146826246
                                        if (newarr[i].ToString() == "-2146826288" ||
                                            newarr[i].ToString() == "-2146826246")
                                            innrtxt = string.Empty;
                                        else
                                            innrtxt = newarr[i].ToString();
                                        xe_str = parent.OwnerDocument.CreateElement("UAString");
                                        xe_str.InnerText = innrtxt;
                                        xe_ual.AppendChild(xe_str);
                                        xe_str = null;
                                    }
                                    xe_uas.AppendChild(xe_ual);
                                    parent.AppendChild(xe_uas);//finally adding to main DOM
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logService.WriteToLogLevel("Could not parse:", LogLevelEnum.Error);
                }
            }
        }

        public object GetReturnValues(string objname)//14jun2013
        {
            object returnval = null;

            try
            {
                this._RServer.Evaluate("tmp<-" + objname);
            }
            catch (Exception e)
            {
                logService.WriteToLogLevel("Could not execute: < " + objname + " >", LogLevelEnum.Error);
            }
            if (false)//this._RServer.GetErrorText() != RCommandReturn.Success)
            {

            }
            else if (!Conversions.ToBoolean(this._RServer.Evaluate("is.null(tmp)").AsLogical()[0]))
            {
                returnval = this._RServer.Evaluate("tmp").AsList();
            }
            return returnval;
        }

        private UADataType getUADataTypeFromName(string typeName)
        {
            UADataType DataType = UADataType.UAUnKnown;
            switch (typeName)
            {
                case "String":
                    DataType = UADataType.UAString;
                    break;
                case "String[]":
                    DataType = UADataType.UAStringList;
                    break;

                case "Double":
                    DataType = UADataType.UADouble;
                    break;
                case "Double[]":
                    DataType = UADataType.UADoubleList;
                    break;
                case "Double[,]":
                case "Object[,]":
                case "String[,]":
                    DataType = UADataType.UADoubleMatrix;
                    break;

                case "Int16":
                case "Int32":
                case "Int64":
                    DataType = UADataType.UAInt;
                    break;

                case "Int16[]":
                case "Int32[]":
                case "Int64[]":
                    DataType = UADataType.UAIntList;
                    break;

                case "Int16[,]":
                case "Int32[,]":
                case "Int64[,]": // here we set 'Double' so that when we FillTable this could be found in XML path string. 'Int' can't be found right now.
                    DataType = UADataType.UADoubleMatrix; ;//UADataType.UAIntMatrix; // 09Jan2013
                    break;

                case "Object[]":
                    DataType = UADataType.UAList;
                    break;
                case "Table[]":
                    DataType = UADataType.UATableList;
                    break;
                case "DataFrame":  ///03Jul2013
                    ////// We can come up with some new type here but then we would need to fix OutputHelper's GetMetaData
                    DataType = UADataType.UADoubleMatrix;//29Apr2014 UADataType.UADataFrame; trying to depricate UADataFrame
                    break;
            }

            return DataType;
        }

        private string getElementTypeName(UADataType dataType)
        {
            return dataType.ToString();
        }
        #endregion

        #region Command Execution
        public XmlDocument EvaluateToXml(string commandString)
        {
            XmlDocument returnValue;
            try
            {
                //16Apr2013///
                bool batchcommand = false;

                try
                {
                    if (!batchcommand)
                    {

                        bool runit = true;
                        if(runit)
                        this._RServer.Evaluate("tmp<-" + commandString); // executing R Command with no left-hand var
                    }
                    else
                        this._RServer.Evaluate(commandString);///16Apr2013 for commands like a<-somfun(..) and b = some expr. left hand var exists.
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error executing:\n"+commandString+"\n" + ex.Message, "R Syntax Execution Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    logService.WriteToLogLevel("Could not execute:\n " + commandString , LogLevelEnum.Error);
                    return null;
                }
            }
            catch (Exception e)
            {
                string errm = "RDotNet: Error message not implemented";// this._RServer.GetErrorText();
                logService.WriteToLogLevel("Could not execute: < " + commandString + " >", LogLevelEnum.Error);
            }
            if (false)//this._RServer.GetErrorText() != RCommandReturn.Success)
            {
                returnValue = null; // new ResultDataItem("Error: " + this._RServer.GetErrorText());

            }
            else if (!Conversions.ToBoolean(this._RServer.Evaluate("is.null(tmp)").AsLogical()[0]))
            {
                returnValue = ParseToXmlDocument("tmp");
            }
            else
            {
                returnValue = null; // new ResultDataItem("No Result - Check Command");
            }

            return returnValue;
        }

        //for R Dot net only
        public string[] GetRow(string command)
        {
            string[] rowdata = null;
            SymbolicExpression se = null;
            CharacterVector cv = null;
            try
            {

                se = this._RServer.Evaluate(command);
                cv = se.AsCharacter();//Dataset[idx,]
                rowdata = cv.ToArray();
                //rowdata = new string[] { command+"one", "two", "three", "four" };
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("command: '"+command+"' "+ex.Message, LogLevelEnum.Error);
            }
            return rowdata;
        }
        object returnVal = null;
        ///14Jun2013 For new R framework. We need XML doc for errors as well as additional result value
        public UAReturn EvaluateToUAReturn(string commandString)
        {
            UAReturn returnRecults = new UAReturn();
            returnRecults.CommandString = commandString;
            XmlDocument returnErrWarn;

            try
            {
                //16Apr2013///
                bool batchcommand = false;
                if (!batchcommand)
                    this._RServer.Evaluate("tmp<-" + commandString); // executing R Command with no left-hand var
                else
                    this._RServer.Evaluate(commandString);///16Apr2013 for commands like a<-somfun(..) and b = some expr. left hand var exists.
            }
            catch (Exception e)
            {
                string errm = "R.NET Error Msg not implemented";// this._RServer.GetErrorText();
                logService.WriteToLogLevel("Could not execute: < " + commandString + " >", LogLevelEnum.Error);
            }
            if (false)//this._RServer.GetErrorText() != RCommandReturn.Success)
            {
                returnErrWarn = null;

            }
            else if (!Conversions.ToBoolean(this._RServer.Evaluate("is.null(tmp)").AsLogical()[0]))
            {
                returnErrWarn = ParseToXmlDocument("tmp");
                ///here returnErrWarn will contain DOM. Having Error/Warn and Return results
            }
            else
            {
                returnErrWarn = null;
            }

            returnRecults.Data = returnErrWarn;
            returnRecults.SimpleTypeData = returnVal;// "Put results here. Should be used for RCommandStrings and nothing else.
            returnVal = null; // resetting so that old value does not get assigned to result of urrent command under execution.
            return returnRecults;
        }


        public object EvaluateToObject(string commandString, bool hasReturn)// read.spss("", "" )
        {
            object returnValue;

            try
            {
                this._RServer.Evaluate("tmp<-" + commandString); // tmp <-  read.spss("", "" )
            }
            catch (Exception e)
            {
                logService.WriteToLogLevel("Could not execute: < " + commandString + " >", LogLevelEnum.Error);
            }
            if (false)//this._RServer.GetErrorText() != RCommandReturn.Success)
            {
                returnValue = "Error: " + "R.Net's error not impl";//this._RServer.GetErrorText();
            }
            else if (!Conversions.ToBoolean(this._RServer.Evaluate("is.null(tmp)").AsLogical()[0]))
            {
                SymbolicExpression se = this._RServer.Evaluate("tmp");//.AsList();// list("","" ....)
                switch (se.Type.ToString())
                {
                    case "CharacterVector":
                        CharacterVector cv = se.AsCharacter();
                        if (cv.Length == 0)
                        {
                            returnValue = null;
                        }
                        else if (cv.Length == 1)
                        {
                            returnValue = cv[0];
                        }
                        else
                        {
                            returnValue =  cv.ToArray();
                        }
                        break;
                    case "IntegerVector":
                        IntegerVector iv = se.AsInteger();
                        if (iv.Length == 0)
                        {
                            returnValue = null;
                        }
                        else if (iv.Length == 1)
                        {
                            returnValue = iv[0];
                        }
                        else
                        {
                            returnValue =  iv.ToArray();
                        }
                        break;
                    case "NumericVector":
                        NumericVector nv = se.AsNumeric();
                        if (nv.Length == 0)
                        {
                            returnValue = null;
                        }
                        else if (nv.Length == 1)
                        {
                            returnValue = nv[0];
                        }
                        else
                        {
                            returnValue = nv.ToArray();
                        }
                        break;
                    case "LogicalVector":
                        LogicalVector lv = se.AsLogical();
                        if (lv.Length == 0)
                        {
                            returnValue = null;
                        }
                        else if (lv.Length == 1)
                        {
                            returnValue = lv[0];
                        }
                        else
                        {
                            returnValue = lv.ToArray();
                        }
                        break;
                    default:
                        returnValue = null;
                        break;
                }
            }
            else
            {
                returnValue = "No Result - Check Command";
            }

            return returnValue;
        }

        //05Mar2015 for fetching all column properties at once
        public SymbolicExpression EvaluateToSymExp(string commandString)
        {
            SymbolicExpression returnValue;

            try
            {
                this._RServer.Evaluate("tmp<-" + commandString); // tmp <-  read.spss("", "" )
            }
            catch (Exception e)
            {
                logService.WriteToLogLevel("Could not execute: < " + commandString + " >", LogLevelEnum.Error);
            }
            if (!Conversions.ToBoolean(this._RServer.Evaluate("is.null(tmp)").AsLogical()[0]))
            {
                returnValue = this._RServer.Evaluate("tmp");//.AsList();// list("","" ....)
            }
            else
            {
                returnValue = null;
            }

            return returnValue;
        }
        /// <summary>
        /// Syntax Editor will use this
        /// </summary>
        /// <param name="commandString"></param>
        public object SyntaxEditorEvaluateToObject_old(string commandString, bool hasReturn, bool hasUAReturn)// read.spss("", "" )
        {
            object returnValue = null;
            dynamic dy = null;
            try
            {
                //21Jun2013 Following if-else ladder /// earlier above commented one was in use
                if (hasReturn)
                {
                    dy = this._RServer.Evaluate(commandString).AsList();
                    if (dy != null)
                        returnValue = dy;
                }
                else if (hasUAReturn)
                {
                    returnValue = this.EvaluateToUAReturn(commandString);
                }
                else//no return
                {
                    string serr = "R.Net Error not imple";// this._RServer.GetErrorText();
                    if (serr != null && serr.Length > 0)
                    { }
                    this._RServer.Evaluate(commandString);
                }

            }
            catch (Exception e)
            {
                if (e != null)
                {

                }
                if (commandString.Contains("readLines("))
                {
                    returnValue = "EOF";// to show the end of file.
                }
                else if (false)
                {
                    returnValue = "Error: " + "No Err impl, R.net";// this._RServer.GetErrorText();
                }
            }

            return returnValue;
        }

        //R.Net version of SyntaxEditorEvaluateToObject
        public object SyntaxEditorEvaluateToObject(string commandString, bool hasReturn, bool hasUAReturn)// read.spss("", "" )
        {
            object returnValue = null;
            dynamic dy = null;
            CharacterVector cvec;
            try
            {
                if (hasReturn)
                {
                    cvec = this._RServer.Evaluate(commandString).AsCharacter();
                    if (cvec != null)
                    {
                        if (cvec.Length > 1)
                        {
                            returnValue = cvec.ToArray();
                        }
                        if (cvec.Length == 1)
                        {
                            returnValue = cvec[0];
                        }
                        
                    }
                }
                else if (hasUAReturn)
                {
                    returnValue = this.EvaluateToUAReturn(commandString);
                }
                else//no return
                {
                    string serr = "R.Net Error not imple";// this._RServer.GetErrorText();
                    if (serr != null && serr.Length > 0)
                    { }
                    this._RServer.Evaluate(commandString);
                }

            }
            catch (Exception e)
            {
                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Exception Executing : " + commandString, LogLevelEnum.Info);
                if (AdvancedLogging) logService.WriteToLogLevel("ExtraLogs: Exception : " + e.Message, LogLevelEnum.Info);
                if (e != null)
                {
                    string msg = e.Message.Replace("\"", "'");
                    returnValue = "Error:" + msg;
                }
                if (commandString.Contains("readLines("))
                {
                    returnValue = "EOF";// to show the end of file.
                }
                else if (false)
                {
                    returnValue = "Error: " + "No Err impl, R.net";// this._RServer.GetErrorText();
                }
            }

            return returnValue;
        }

        public string EvaluateNoReturn(string commandString)
        {
            string errmsg = string.Empty;
            try
            {
                this._log.ClearErrorConsole();
                this._RServer.Evaluate(commandString);
                if (this._log.LastError != null && this._log.LastError.Length > 0)
                    errmsg = this.GetErrorText();//this flushes the old error. Cleaned for next.
            }
            catch (Exception ex)
            {
                if (this._log.LastError != null && this._log.LastError.Length > 0)
                    errmsg = this.GetErrorText();//this flushes the old error. Cleaned for next.
                else
                    errmsg = "Exception occurred but no error message from R.";// _RServer.GetErrorText();// _log.LastError;
                LastException = ex;
                logService.WriteToLogLevel("Could not execute: < " + commandString + " >", LogLevelEnum.Error);
            }
            return errmsg;
        }

        public string EvaluateToString(string commandString)
        {
            bool flag = true;

            string resultValueString = string.Empty;

            bool hasReturnVariable = false;

            string subCommand = string.Empty;
            string subCommandReturnVariable = string.Empty;

            try
            {
                subCommand = commandString.Substring(0, commandString.IndexOf("("));
            }
            catch (Exception exception1)
            {
                resultValueString = _log.LastError;
                ProjectData.SetProjectError(exception1);
                subCommand = "";
                ProjectData.ClearProjectError();
            }
            if (commandString.Contains("<-"))
            {
                subCommand = subCommand.Substring(subCommand.IndexOf("-") + 1).Trim();
                hasReturnVariable = true;
                subCommandReturnVariable = commandString.Substring(0, commandString.IndexOf("<")).Trim();
            }
            if (subCommand.Contains("="))
            {
                subCommand = subCommand.Substring(subCommand.IndexOf("=") + 1).Trim();
                hasReturnVariable = true;
                subCommandReturnVariable = commandString.Substring(0, commandString.IndexOf("=")).Trim();
            }

            if (commandString.StartsWith("#"))
            {
                subCommand = "Comment";
            }

            switch (subCommand)
            {
                case "Comment":
                    resultValueString = "";
                    break;

                case "help":
                case "help.search":
                    try
                    {
                        resultValueString = Conversions.ToString(this._RServer.Evaluate(commandString).AsCharacter()[0]);
                    }
                    catch (Exception exception2)
                    {
                        ProjectData.SetProjectError(exception2);
                        Exception exception = exception2;
                        resultValueString = "Error!";
                        flag = true;
                        ProjectData.ClearProjectError();
                        logService.WriteToLogLevel("Could not execute and convert to string: < " + commandString + " >", LogLevelEnum.Error);
                    }
                    break;

                case "library":
                    try
                    {
                        if (false)//RuntimeHelpers.GetObjectValue(this._RServer.Evaluate(commandString)) == null)
                        {
                            resultValueString = "Could not find library!";
                        }
                        else
                        {
                            resultValueString = "Library Loaded";
                        }
                    }
                    catch (Exception exception3)
                    {
                        ProjectData.SetProjectError(exception3);
                        resultValueString = "Could not find library!";
                        ProjectData.ClearProjectError();
                        logService.WriteToLogLevel("Could not execute: < " + commandString + " >", LogLevelEnum.Error);
                    }
                    break;

                case "rm":
                case "plot":
                case "hist":
                case "scatterplot3d":
                case "scatter3d":
                case "abline":
                case "edit":
                case "legend":
                case "par":
                case "barplot":
                    try
                    {
                        this._RServer.Evaluate(commandString);
                        resultValueString = "Done!";
                        if (false)//this._RServer.GetErrorText() != "no error")
                        {
                            resultValueString = "Error: " + "not imple for RDotNet";//this._RServer.GetErrorText();
                        }
                    }
                    catch (Exception exception4)
                    {
                        ProjectData.SetProjectError(exception4);
                        resultValueString = "There was an error!";
                        flag = true;
                        ProjectData.ClearProjectError();
                        logService.WriteToLogLevel("Could not execute:< " + commandString + " >", LogLevelEnum.Error);
                    }
                    break;

                default:
                    if (subCommand == "model")
                    {
                        object obj4 = null;
                        try
                        {
                            if (subCommandReturnVariable == "")
                            {
                                subCommandReturnVariable = "Temporary_Model";
                                this._RServer.Evaluate("Temporary_Model<-" + commandString);
                            }
                            else
                            {
                                this._RServer.Evaluate(commandString);
                            }
                            if (false)//this._RServer.GetErrorText() != "no error")
                            {
                                resultValueString = "Error: " + "not imple in RDotNet";// this._RServer.GetErrorText();
                            }
                            else
                            {

                                
                            }
                            break;
                        }
                        catch (Exception exception5)
                        {
                            logService.WriteToLogLevel("Could not execute:< " + commandString + " >", LogLevelEnum.Error);
                            ProjectData.SetProjectError(exception5);
                            resultValueString = "There was an error!";
                            flag = true;
                            ProjectData.ClearProjectError();
                            break;
                        }
                    }
                    if (hasReturnVariable)
                    {
                        try
                        {
                            this._RServer.Evaluate(commandString);
                            resultValueString = "Done!";
                            if (false)//this._RServer.GetErrorText() != "no error")
                            {
                                resultValueString = "Error: " + "no err impl in R.Net";// this._RServer.GetErrorText();
                                flag = true;
                            }
                            break;
                        }
                        catch (Exception exception6)
                        {
                            logService.WriteToLogLevel("Could not execute: < " + commandString + " >", LogLevelEnum.Error);
                            ProjectData.SetProjectError(exception6);
                            resultValueString = "There was an error!";
                            ProjectData.ClearProjectError();
                            break;
                        }
                    }
                    try
                    {
                        this._RServer.Evaluate("tmp<-" + commandString);//loading/closing dataset. maybe others commnd too
                        if (_log.LastError != null && _log.LastError.Trim().Length > 0)
                            resultValueString = GetErrorText();
                        else
                            resultValueString = string.Empty;
                        if (false)//this._RServer.GetErrorText() != "no error")
                        {
                            resultValueString = "Error: " + "No error R.net";//this._RServer.GetErrorText();
                            flag = true;
                        }
                        else if (!Conversions.ToBoolean(this._RServer.Evaluate("is.null(tmp)").AsLogical()[0]))
                        {

                        }
                        else
                        {
                            if(string.IsNullOrEmpty(resultValueString))
                                resultValueString = "No Result";// "No Result - Check Command";
                        }
                    }
                    catch (Exception exception7)
                    {
                        if (_log.LastError != null && _log.LastError.Trim().Length > 0)
                            resultValueString = GetErrorText();
                        logService.WriteToLogLevel("Could not execute: < " + commandString + " > ", LogLevelEnum.Error, exception7);
                        ProjectData.SetProjectError(exception7);
                        resultValueString = "There was an error!";
                        flag = true;
                        ProjectData.ClearProjectError();
                    }
                    break;
            }
            if (hasReturnVariable)
            {
                try
                {
                    if (Conversions.ToBoolean(this._RServer.Evaluate("is.data.frame(" + subCommandReturnVariable + ")").AsLogical()[0]))
                    {

                    }
                }
                catch (Exception exception8)
                {
                    logService.WriteToLogLevel("Could not execute and convert to bool:< " + "is.data.frame(" + subCommandReturnVariable + ")" + " >", LogLevelEnum.Error);
                    ProjectData.SetProjectError(exception8);
                    flag = true;
                    ProjectData.ClearProjectError();
                }
            }
            if (flag)
            {
                return resultValueString;
            }
            else
            {
                return "Done!";
            }
        }

        private string InterpretReturn(object objRtr, string Command)
        {
            int num;
            object instance = null;
            string str2;
            try
            {
                this._RServer.Evaluate("tmp<-names(" + Command + ")");
                if (!(bool)this._RServer.Evaluate("is.null(tmp)").AsLogical()[0])
                {
                    instance = (this._RServer.Evaluate("tmp").AsList());
                }
            }
            catch (Exception exception1)
            {
                logService.WriteToLogLevel("Could not execute : < " + Command + " >", LogLevelEnum.Error);
                LastException = exception1;
            }

            switch (objRtr.GetType().ToString())
            {
                case "System.String":
                    return objRtr.ToString();

                case "System.Double":
                    return Conversions.ToDouble(objRtr).ToString();

                case "System.Int32":
                    return Conversions.ToInteger(objRtr).ToString();

                case "System.Int32[]":
                    {
                        str2 = "<table border=1 cellspacing=1>";
                        if ((instance != null) && (instance.GetType().ToString() == "System.String[]"))
                        {
                            str2 = str2 + "<tr>";
                            int num7 = ((Array)instance).Length - 1;
                            for (num = 0; num <= num7; num++)
                            {
                                str2 = str2 + "<th>" + NewLateBinding.LateIndexGet(instance, new object[] { num }, null).ToString() + "</th>";
                            }
                            str2 = str2 + "</tr>";
                        }
                        str2 = str2 + "<tr>";
                        int num8 = ((int[])objRtr).Length - 1;
                        for (num = 0; num <= num8; num++)
                        {
                            str2 = str2 + "<td align='right'>" + NewLateBinding.LateIndexGet(objRtr, new object[] { num }, null).ToString() + "</td>";
                        }
                        return (str2 + "</tr></table>");
                    }
                case "System.Int32[,]":
                    {
                        Array array = (Array)objRtr;
                        str2 = "<table border=1 cellspacing=1>";
                        int upperBound = array.GetUpperBound(0);
                        for (num = 0; num <= upperBound; num++)
                        {
                            str2 = str2 + "<tr>";
                            int num10 = array.GetUpperBound(1);
                            for (int i = 0; i <= num10; i++)
                            {
                                str2 = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(str2 + "<td>", NewLateBinding.LateIndexGet(objRtr, new object[] { num, i }, null)), "</td>"));
                            }
                            str2 = str2 + "</tr>";
                        }
                        return (str2 + "</table>");
                    }
                case "System.Double[]":
                    {
                        str2 = "<table border=1 cellspacing=1>";
                        if ((instance != null) && (instance.GetType().ToString() == "System.String[]"))
                        {
                            str2 = str2 + "<tr>";
                            int num11 = ((Array)instance).Length - 1;
                            for (num = 0; num <= num11; num++)
                            {
                                str2 = str2 + "<th>" + NewLateBinding.LateIndexGet(instance, new object[] { num }, null).ToString() + "</th>";
                            }
                            str2 = str2 + "</tr>";
                        }
                        str2 = str2 + "<tr>";
                        int num12 = ((double[])objRtr).Length - 1;
                        for (num = 0; num <= num12; num++)
                        {
                            str2 = str2 + "<td align='right'>" + NewLateBinding.LateIndexGet(objRtr, new object[] { num }, null).ToString() + "</td>";
                        }
                        return (str2 + "</tr></table>");
                    }
                case "System.Double[,]":
                    {
                        Array array2 = (Array)objRtr;
                        str2 = "<table border=1 cellspacing=1>";
                        int num13 = array2.GetUpperBound(0);
                        for (num = 0; num <= num13; num++)
                        {
                            str2 = str2 + "<tr>";
                            int num14 = array2.GetUpperBound(1);
                            for (int j = 0; j <= num14; j++)
                            {
                                str2 = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(str2 + "<td align='right'>", NewLateBinding.LateIndexGet(objRtr, new object[] { num, j }, null)), "</td>"));
                            }
                            str2 = str2 + "</tr>";
                        }
                        return (str2 + "</table>");
                    }
                case "System.String[]":
                    {
                        str2 = "<table border=1 cellspacing=1>";
                        if ((instance != null) && (instance.GetType().ToString() == "System.String[]"))
                        {
                            str2 = str2 + "<tr>";
                            int num15 = ((Array)instance).Length - 1;
                            for (num = 0; num <= num15; num++)
                            {
                                str2 = str2 + "<th>" + NewLateBinding.LateIndexGet(instance, new object[] { num }, null).ToString() + "</th>";
                            }
                            str2 = str2 + "</tr>";
                        }
                        str2 = str2 + "<tr>";
                        int num16 = ((string[])objRtr).Length - 1;
                        for (num = 0; num <= num16; num++)
                        {
                            str2 = str2 + "<td>" + NewLateBinding.LateIndexGet(objRtr, new object[] { num }, null).ToString() + "</td>";
                        }
                        return (str2 + "</tr></table>");
                    }
                case "System.String[,]":
                    {
                        Array array3 = (Array)objRtr;
                        str2 = "<table border=1 cellspacing=1>";
                        int num17 = array3.GetUpperBound(0);
                        for (num = 0; num <= num17; num++)
                        {
                            str2 = str2 + "<tr>";
                            int num18 = array3.GetUpperBound(1);
                            for (int k = 0; k <= num18; k++)
                            {
                                str2 = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(str2 + "<td>", NewLateBinding.LateIndexGet(objRtr, new object[] { num, k }, null)), "</td>"));
                            }
                            str2 = str2 + "</tr>";
                        }
                        return (str2 + "</table>");
                    }
                case "System.Object[]":
                    {
                        object objectValue = (this._RServer.Evaluate("names(" + Command + ")").AsCharacter());
                        str2 = "<table border=1 cellspacing=1>";
                        if (objectValue.GetType().ToString() == "System.String[]")
                        {
                            str2 = str2 + "<tr>";
                            int num19 = ((Array)objectValue).Length - 1;
                            for (num = 0; num <= num19; num++)
                            {
                                str2 = str2 + "<th>" + NewLateBinding.LateIndexGet(objectValue, new object[] { num }, null).ToString() + "</th>";
                            }
                            str2 = str2 + "</tr>";
                        }
                        str2 = str2 + "<tr>";
                        int num20 = ((object[])objRtr).Length - 1;
                        for (num = 0; num <= num20; num++)
                        {
                            str2 = str2 + "<td>" + NewLateBinding.LateIndexGet(objRtr, new object[] { num }, null).ToString() + "</td>";
                        }
                        return (str2 + "</tr></table>");
                    }
            }
            return objRtr.ToString();
        }

        #endregion

        #region R Environment Manipulation
        public bool IsLoaded(string Package)
        {
            bool isLoaded = false;
            try
            {
                object objectValue = (this.RawEvaluate("search()"));
                if (objectValue != null)
                {
                    IEnumerator enumerator = null;
                    try
                    {
                        enumerator = ((IEnumerable)objectValue).GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            string str = enumerator.Current.ToString();
                            if (str.StartsWith("package:") && (str.Substring(str.IndexOf(":") + 1) == Package))
                            {
                                isLoaded = true;
                                break;
                            }
                        }
                    }
                    finally
                    {
                        if (enumerator is IDisposable)
                        {
                            (enumerator as IDisposable).Dispose();
                        }
                    }
                }

            }
            catch
            {
                isLoaded = false;
                logService.WriteToLogLevel("Could not execute : search/package commands ", LogLevelEnum.Error);
            }
            return isLoaded;
        }

        public object RawEvaluate(string command)
        {
            object obj;
            try
            {
                obj = this._RServer.Evaluate(command).AsCharacter().ToArray();//.AsList();
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("Could not execute :< " + command + " >", LogLevelEnum.Error);
                LastException = ex;
                obj = null;
            }
            return obj;
        }

        //this is specifically for getting 'class' of a column( of a dataset)
        public string RawEvaluateGetstring(string command)
        {
            object obj=null;
            try
            {
                SymbolicExpression se = this._RServer.Evaluate(command);
                
                if(se!=null && se.AsCharacter()!=null)
                {
                    obj = se.AsCharacter()[0];//.AsList();
                }
            }
            catch (Exception ex)
            {
                logService.WriteToLogLevel("Could not execute :< " + command + " >", LogLevelEnum.Error);
                LastException = ex;
                obj = null;
            }
            if (obj != null)
                return obj.ToString();
            else
                return null;
        }

        public bool SetVariable(string VariableName, string command)
        {
            bool flag = false;
            try
            {

                flag = true;
            }
            catch
            {
                logService.WriteToLogLevel("Could not execute :< " + command + " >", LogLevelEnum.Error);
            }
            return flag;
        }
        #endregion

        #region Error Handling
        public string GetErrorText()
        {
            string errorText;
            try
            {
                errorText = this._log.LastError; //29Jun2015 these 2 new lines added and 2 lines above commented 
                _log.ClearErrorConsole();
            }
            catch
            {
                logService.WriteToLogLevel("Could not find error text : ", LogLevelEnum.Error);
                errorText = string.Empty;
            }
            return errorText;
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            logService.WriteToLogLevel("Disposing RService...", LogLevelEnum.Fatal);
            this._RServer.Close();//19feb2013
        }

        #endregion

    }
}


