﻿using System.Collections.Generic;
using System.IO;

namespace BSky.Statistics.Common
{
    public enum ServerDataSourceTypeEnum : uint { Unknown=0, SPSS = 1, CSV = 2, XLS = 3, XLSX = 4, DBF = 5, RDATA = 6, ROBJ = 7 } //TXT = 7
    // Remove ROBJ in above line when new uadatapackage is in use

    public class ServerDataSource
    {

        public ServerDataSourceTypeEnum DataSetType { get; set; }
        public int RowCount { get; set; }

        public string Extension { get; private set; }
        public bool HasHeader { get; set; }
        public string FieldSperator { get; set; }
        
        public CommandDispatcher Dispatcher { get; private set; }
        public string Name { get; private set; }
        public string FileNameWithPath { get; private set; }
        public string FileName { get; private set; }
        public string SheetName { get; private set; }//29Apr2015
        public int MaxFactors { get; set; }
        public List<DataSourceVariable> Variables = new List<DataSourceVariable>();

        //public ServerDataSource(CommandDispatcher dispatcher, string fileName, string datasetname)
        //{

        //    this.Dispatcher = dispatcher;
        //    this.FileNameWithPath = fileName;//.ToLower();
        //    this.FileName = System.IO.Path.GetFileName(FileNameWithPath);//filename
        //    this.Name = datasetname;//dataset name assigned by application to the opened dataset(.sav) file
        //    this.Extension = Path.GetExtension(fileName).Replace('.', ' ').Trim(); //fileName.Substring(fileName.LastIndexOf(".")+1);

        //}

        public ServerDataSource(CommandDispatcher dispatcher, string fileName, string datasetname, string sheetname = "")
        {

            this.Dispatcher = dispatcher;
            this.FileNameWithPath = fileName;//.ToLower();
            this.FileName = System.IO.Path.GetFileName(FileNameWithPath);//filename
            this.SheetName = sheetname;//29Apr2015
            this.Name = datasetname;//dataset name assigned by application to the opened dataset(.sav) file
            this.Extension = Path.GetExtension(fileName).Replace('.', ' ').Trim(); //fileName.Substring(fileName.LastIndexOf(".")+1);

        }

        public void Save()
        {
            
        }
       
        public void SaveAs(string fileName)//was empty function earlier
        {
            this.Dispatcher.DataSourceLoad(this, null);//Anil
        }
        
        public void Load()
        {
            this.Dispatcher.DataSourceLoad(this, null);
        }
        
        public void Close(bool saveChanges)
        {
            Dispatcher.DataSourceClose(this);
        }

        public DataSource ToClientDataSource()
        {
           return new DataSource()
            {
                IsLoaded = true,
                Name = this.Name,
                SheetName = this.SheetName,  //29Apr2015
                Extension = this.Extension,
                FieldSperator = this.FieldSperator,
                HasHeader = this.HasHeader,
                FileName = this.FileNameWithPath,
                Variables = this.Variables,
                RowCount = this.RowCount,
                maxfactor = this.MaxFactors
            };
        }

        public UAReturn ReadRows(int startRow, int endRow)
        {
            return this.Dispatcher.DataSourceReadRows(this, startRow, endRow);

        }
      
        public UAReturn ReadCell(int row, int col) { return  this.Dispatcher.DataSourceReadCell(this, row, col); }

        public UAReturn ReadRow(int row) { return this.Dispatcher.DataSourceReadRow(this, row); } //23Jan2014 read a row, at once
    }
}
