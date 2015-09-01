using System.Collections.Generic;
using System.Linq;
using BSky.Statistics.Service.Engine.Interfaces;
using BSky.Statistics.Common;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using System;
using BSky.XmlDecoder;
using BSky.Interfaces.Model;
using BSky.Interfaces.Interfaces;

namespace BlueSky.Services
{
    public class DataService : IDataService
    {
        private Dictionary<string, DataSource> _datasources = new Dictionary<string,DataSource>();
        private Dictionary<string, string> _datasourcenames = new Dictionary<string, string>();//05Mar2014
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012

        int SessionDatasetCounter = 1;
        private IAnalyticsService _analyticService;
        public DataService(IAnalyticsService analyticsService)
        {
            _analyticService = analyticsService;
        }


        #region IDataService Members


        public DataSource NewDataset()//03Jan2014
        {
            string datasetname = "Dataset" + SessionDatasetCounter;//(_datasources.Keys.Count + 1);//can also be filename without path and extention

            //15Jun2015 if Dataset is created and loaded from syntax UI SessionDatasetCounter can have issues as it may not be increamented when
            // datasetset is loaded from syntax
            if (_datasources.Keys.Contains(datasetname))
                return _datasources[datasetname];

            UAReturn datasrc = _analyticService.EmptyDataSourceLoad(datasetname, datasetname);//second pram was full path filename on disk
            if (datasrc.Datasource == null)
            {
                logService.WriteToLogLevel("Could not open: " + datasetname + ".\nInvalid format OR issue related to R.Net server.", LogLevelEnum.Error);
                //string[,] errwarnmsg=OutputHelper.GetMetaData(1, "normal");//08Jun2013
                ////AnalyticsData data = new AnalyticsData();
                ////data.Result = datasrc;
                ////if (!data.Result.Success)
                ////{
                ////    OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;//new line
                ////    IOutputWindow _outputWindow = owc.ActiveOutputWindow;//To get active ouput window to populate analysis.AD.
                ////    _outputWindow.Show();
                ////    OutputHelper.Reset();
                ////    _outputWindow.AddAnalyis(data);
                ////}
                //SendToOutput(datasrc);

                return null;
            }
            //datasrc.CommandString = "Open Dataset";//21Oct2013
            DataSource ds = datasrc.Datasource.ToClientDataSource();
            if (ds != null)//03Dec2012
            {
                //_datasources.Add(ds.FileName, ds);///key filename
                ////_datasourcenames.Add(datasetname, ds.FileName);

                _datasources.Add(ds.FileName + ds.SheetName, ds);///key filename
                _datasourcenames.Add(datasetname + ds.SheetName, ds.FileName);//5Mar2014
            }
            ///incrementing dataset counter //// 14Feb2013
            SessionDatasetCounter++;
            SendToOutput(datasrc);
            return ds;
        }

        public DataSource Open(string filename, string sheetname)
        {
            if (sheetname==null || sheetname.Trim().Length == 0) //29Apr2015 just to make sure sheetname should have valid chars and not spaces.
                sheetname = string.Empty;
            //int i = 10, j = 0;
            //if (i > 0) i = i / j;
            //filename = filename.ToLower();
            if (_datasources.Keys.Contains(filename+sheetname))
                return _datasources[filename+sheetname];
            string datasetname = "Dataset" + SessionDatasetCounter;//(_datasources.Keys.Count + 1);//can also be filename without path and extention
            UAReturn datasrc = _analyticService.DataSourceLoad(datasetname, filename, sheetname);
            if (datasrc == null)
            {
                logService.WriteToLogLevel("Could not open: " + filename , LogLevelEnum.Error);
                return null;
            }
            else if (datasrc!= null && datasrc.Datasource == null)
            {
                if (datasrc.Error != null && datasrc.Error.Length > 0)
                {
                    logService.WriteToLogLevel("Could not open: " + filename + ".\n"+datasrc.Error, LogLevelEnum.Error);
                }
                else
                    logService.WriteToLogLevel("Could not open: " + filename + ".\nInvalid format OR issue related to R.Net server.", LogLevelEnum.Error);
                //string[,] errwarnmsg=OutputHelper.GetMetaData(1, "normal");//08Jun2013
    ////AnalyticsData data = new AnalyticsData();
    ////data.Result = datasrc;
    ////if (!data.Result.Success)
    ////{
    ////    OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;//new line
    ////    IOutputWindow _outputWindow = owc.ActiveOutputWindow;//To get active ouput window to populate analysis.AD.
    ////    _outputWindow.Show();
    ////    OutputHelper.Reset();
    ////    _outputWindow.AddAnalyis(data);
    ////}
                //SendToOutput(datasrc);
                DataSource dsnull = new DataSource(){ Message = datasrc.Error};
                return dsnull;
            }


            /////14Jun2015 ADD alogic here to check for duplicate keys before moving further


            //datasrc.CommandString = "Open Dataset";//21Oct2013
            DataSource ds = datasrc.Datasource.ToClientDataSource();
            if(ds!=null)//03Dec2012
            {
                _datasources.Add(ds.FileName + ds.SheetName, ds);///key filename
                _datasourcenames.Add(datasetname + ds.SheetName, ds.FileName);//5Mar2014
            }
                                                  
            ///incrementing dataset counter //// 14Feb2013
            SessionDatasetCounter++;
            SendToOutput(datasrc);
            return ds;
        }

        //In Datagrid tabs the tab title looks like this : Cars.sav(DatasetN), where N is any integer.
        //So here DatasetN is the object in memory that is dataframe( or similar).
        //Cars.sav is just extra info about the Dataset you tried to open. This extra info could be a disk filename
        // or it could also be just a memory object name if memory object name is different from 'DatasetN' style of naming
        // eg.. mydf(mydf) , Dataset1(Dataset1), Cars.sav(Dataset2), mydata.rdata(Dataset3) 
        // in above example mydf, Dataset1 Dataset2, Dataset3 , all R objects exists in R memory.

        //Wrong way of naming would look something like this : mydf(Dataset1)
        // mydf is not a disk file, so lets assume it may be R dataframe obj in memory, but then we always put memory object 
        // name within round brackets, so, if (Dataset1) is a memory object, the extra info 'mydf' does not make sense. 
        //So 'mydf' should be named as 'Dataset1' instead.
        //Right thing would be either one of two mydf(mydf) or Dataset1(Dataset1) for non-disk datasets

        public DataSource OpenDataframe(string dframename, string sheetname) //13Feb2014
        {
            UAReturn datasrc = null;
            DataSource ds = null;
            if (!isDatasetNew(dframename+sheetname)) // if data.frame with same name already loaded in C1DataGrid
            {
                string filename = _datasourcenames[dframename + sheetname];
                datasrc = new UAReturn();
                datasrc = _analyticService.DataFrameLoad(filename, dframename, sheetname);
                //////////////
                if (datasrc == null)
                {
                    logService.WriteToLogLevel("Could not open: " + filename, LogLevelEnum.Error);
                    return null;
                }
                else if (datasrc != null && datasrc.Datasource == null)
                {
                    if (datasrc.Error != null && datasrc.Error.Length > 0)
                    {
                        logService.WriteToLogLevel("Could not refresh/open: " + filename + ".\n" + datasrc.Error, LogLevelEnum.Error);
                    }
                    else
                        logService.WriteToLogLevel("Could not refresh/open: " + dframename + ".\nInvalid format OR issue related to R.Net server.", LogLevelEnum.Error);

                    DataSource dsnull = new DataSource() { Message = datasrc.Error };
                    return dsnull;
                }

                datasrc.CommandString = "Refresh Dataframe";
                try
                {
                    if (_datasources.Keys.Contains(filename + sheetname))
                        ds = _datasources[filename + sheetname];
                    else //no need to check if it exists as we alreay checked if its new dataset or not in code above
                        ds = _datasources[_datasourcenames[dframename + sheetname]];
                }
                catch (Exception ex)
                {
                    logService.WriteToLogLevel("Error getting existing DataSource handle", LogLevelEnum.Fatal);
                }
                string existingDatasetname = ds.Name;
                ds = Refresh(ds);
            }
            else // Its New Data.Frame . 
            {
                string datasetname = "Dataset" + SessionDatasetCounter;//dframename;// //(_datasources.Keys.Count + 1);//can also be filename without path and extention

                //15Jun2015 dframename exists in memory so use that name for datagrid tab name enclosed in round brackets (Dataset1) or (df1)
                if (!dframename.Equals(datasetname)) //df2 and Dataset2
                {
                    datasetname = dframename;
                    //use df2 for both because df2 exists in R memory
                    //no need to increament the SessionDatasetCounter as when you try to open a disk file it will have (Dataset2) 
                    // as name and it will not clash with the (df2) name.
                    //incrementing may not harm but there is no need to increment. 

                    //ELSE dframename.Equals(datasetname) like both are say "Dataset2"
                    // use (Dataset2) for both because Dataset2 exists in memory
                    //Also increament the SessionDatasetCounter in this case because now later 
                    //when you open Dataset from disk is should have name Dataset3 and not Dataset2
                }
                datasrc = _analyticService.DataFrameLoad(dframename, datasetname, "");

                if (datasrc == null)
                {
                    logService.WriteToLogLevel("Could not open: " + dframename, LogLevelEnum.Error);
                    return null;
                }
                else if (datasrc != null && datasrc.Datasource == null)
                {
                    if (datasrc.Error != null && datasrc.Error.Length > 0)
                    {
                        logService.WriteToLogLevel("Could not open: " + dframename + ".\n" + datasrc.Error, LogLevelEnum.Error);
                    }
                    else
                        logService.WriteToLogLevel("Could not open: " + dframename + ".\nInvalid format OR issue related to R.Net server.", LogLevelEnum.Error);

                    DataSource dsnull = new DataSource() { Message = datasrc.Error };
                    return dsnull;
                }

                datasrc.CommandString = "Open Dataset";//21Oct2013
                ds = datasrc.Datasource.ToClientDataSource();

                if (ds != null)//03Dec2012
                {
                    //_datasources.Add(ds.FileName, ds);///key filename
                    _datasources.Add(ds.FileName + ds.SheetName, ds);///key filename
                    _datasourcenames.Add(datasetname + ds.SheetName, ds.FileName);//5Mar2014
                }
                ///incrementing dataset counter //// 15Jun2015
                /// if the name of the Dataset created in syntax matches to the Dataset name generated for UI grid.
                if(dframename.Equals(datasetname))
                {
                    SessionDatasetCounter++;
                }
            }
//04Nov2014. Dont show "Open Dataset" before subset command title.   SendToOutput(datasrc);
            return ds;
        }

        public object GetOdbcTableList(string filename)//27Jan2014
        {
            UAReturn tbls = _analyticService.GetOdbcTables(filename);
            return tbls.SimpleTypeData;
        }

        public DataSource Refresh(DataSource datasetname)//25Mar2013
        {
            //filename = filename.ToLower();
            //if (_datasources.Keys.Contains(filename.ToLower()))
            //    return _datasources[filename];
            //string datasetname = "Dataset" + SessionDatasetCounter;
            UAReturn datasrc = _analyticService.DataSourceRefresh(datasetname.Name, datasetname.FileName);
            if (datasrc == null)
            {
                logService.WriteToLogLevel("Could not open:" + datasetname.FileName + ".Invalid format OR issue related to R.Net server.", LogLevelEnum.Error);
                return null;
            }
            DataSource ds = datasrc.Datasource.ToClientDataSource();
            if (ds != null)//03Dec2012
            {
                _datasources.Remove(datasetname.FileName);//Remove old
                _datasources.Add(ds.FileName, ds);///Replace ds with new one
                                                  
                //5Mar2014 No need to do following but we can still do it
                _datasourcenames.Remove(ds.Name);
                _datasourcenames.Add(ds.Name, ds.FileName);
            }

            ///incrementing dataset counter //// 14Feb2013
            //SessionDatasetCounter++;

            return ds;
        }

        public void SaveAs(string filename, DataSource ds)//#1
        {
            string s = ds.Name;//Dataset Name of currently open grid. I guess. So no need to specifically provide it.-Anil
            ds.Changed = false; // dont show popup while closing. Coz its already saved
            string filetype = filename.Substring(filename.LastIndexOf(".") + 1).ToUpper();
            _analyticService.DatasetSaveAs(filename, filetype, ds.Name);//Anil. 
            return;
        }

        public void Close(DataSource ds)//#1
        {
            string datasetName = ds.Name;//Dataset Name of currently open grid. I guess. So no need to specifically provide it.-Anil
            _analyticService.DatasetClose(datasetName);//Anil. 
            if (_datasources.ContainsKey(ds.FileName + ds.SheetName))
            {
                _datasources.Remove(ds.FileName+ ds.SheetName);//remove from the dictionary ///filename
                //5Mar2014
                _datasourcenames.Remove(ds.Name+ ds.SheetName);
            }
            else
                logService.WriteToLogLevel("Key not found. Unable to close:" + ds.FileName, LogLevelEnum.Error);
            return;
        }

        public bool isDatasetNew(string dsname)//17Feb2014
        {
            bool isNew=true;
            if (_datasources.Keys.Contains(dsname)) // if true not new 
                isNew = false;
            else if(_datasourcenames.Keys.Contains(dsname))
            {
                // check partial name ie 'Dataset1'
                //int keycount = _datasources.Keys.Count;
                //for (int i = 0; i < keycount;i++ )
                //{

                //}
                isNew = false;
            }
            return isNew;
        }

        public void editVarGrid(DataSource ds, string colName, string colProp, string newLabel, List<string> newOrder)
        {
            _analyticService.EditVarGrid(ds.Name, colName, colProp, newLabel, newOrder);//Anil. 
            return;
        }

        public void SendToOutput(UAReturn datasrc) // if possible move this function to global space so that it can be used by
        {                                           // open/close/ edit vargrid / edit datagrid etc.. you might want to add few more lines here.
            AnalyticsData data = new AnalyticsData();
            data.Result = datasrc;
            data.AnalysisType = datasrc.CommandString;//21Oct2013

            OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;//new line
            IOutputWindow _outputWindow = owc.ActiveOutputWindow;//To get active ouput window to populate analysis.AD.
            _outputWindow.Show();
            OutputHelper.Reset();
            _outputWindow.AddAnalyis(data);
        }
        #endregion

        //06Dec2013
        #region Package Related
        public UAReturn installPackage(string[] pkgfilenames, bool autoLoad = true, bool overwrite = false)//(string package, string filepath)
        {
           UAReturn r =  _analyticService.PackageInstall(pkgfilenames, autoLoad, overwrite);//(package, filepath);
           //SendToOutput(r);
           return r;
        }
        public UAReturn installCRANPackage(string packagename)
        {
            UAReturn r = _analyticService.CRANPackageInstall(packagename);
            //SendToOutput(r);
            return r;
        }
        public UAReturn installReqPackageCRAN(string packagename)//27Aug2015
        {
            UAReturn r = _analyticService.CRANReqPackageInstall(packagename);
            //SendToOutput(r);
            return r;
        }
        public UAReturn setCRANMirror()
        {
            UAReturn r = _analyticService.setCRANMirror();
            //SendToOutput(r);
            return r;
        }
        public UAReturn loadPackage(string package)
        {
            UAReturn r = _analyticService.PackageLoad(package);
            //SendToOutput(r);
            return r;
        }
        public UAReturn loadPackageFromList(string[] packagenames)
        {
            UAReturn r = _analyticService.ListPackageLoad(packagenames);
            //SendToOutput(r);           
            return r;
        }
        public UAReturn showInstalledPackages()
        {
            UAReturn r = _analyticService.ShowPackageInstalled();
            //SendToOutput(r);
            return r;
        }
        public UAReturn showLoadedPackages()
        {
            UAReturn r = _analyticService.ShowPackageLoaded();
            //SendToOutput(r);
            return r;
        }
        public UAReturn unloadPackage(string[] packagenames)
        {
            UAReturn r = _analyticService.PackageUnload(packagenames);
            //SendToOutput(r);
            return r;
        }
        public UAReturn uninstallPackage(string[] packagenames)
        {
            UAReturn r = _analyticService.PackageUninstall(packagenames);
            //SendToOutput(r);
            return r;
        }
        #endregion
    }
}
