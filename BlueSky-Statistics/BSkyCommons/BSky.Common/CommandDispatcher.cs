using System.Collections.Generic;
using RDotNet;

namespace BSky.Statistics.Common
{
    public abstract class CommandDispatcher
    {

        public abstract DataFrame GetDF(DataSource ds);

        public List<ServerDataSource> DataSources { get; private set; }

        public CommandDispatcher()
        {
            DataSources = new List<ServerDataSource>();
            onLoad();
        }


        public ServerDataSource DataSourceLoad(string sourceName, string filePath)
        {
            //Load datasource
            ServerDataSource ds = new ServerDataSource(this, filePath, sourceName);

            ds.Load();

            this.DataSources.Add(ds);

            return ds;
        }

        public abstract UAReturn DataSourceReadRows(ServerDataSource dataSource, int start, int end);
        
        public abstract UAReturn DataSourceReadCell(ServerDataSource dataSource, int row, int col);

        public abstract UAReturn DataSourceReadRow(ServerDataSource dataSource, int row); //23Jan2014

        public abstract UAReturn EmptyDataSourceLoad(ServerDataSource dataSource);//03Jan2014

        public abstract UAReturn DataSourceLoad(ServerDataSource dataSource, string sheetname);

        public abstract UAReturn DataFrameLoad(ServerDataSource dataSource, string dframe); //13Feb2014

        public abstract UAReturn GetRodbcTables(string fname); //27Jan2014

        public abstract UAReturn DataSourceRefresh(ServerDataSource dataSource);//25Mar2013

        public abstract void onLoad();

        public virtual UAReturn DataSourceClose(ServerDataSource dataSource)
        {
            this.DataSources.Remove(dataSource);

            return new UAReturn();
        }

        public abstract UAReturn DataSourceSave(ServerDataSource dataSource);

        public abstract UAReturn Execute(ServerCommand Command);

        public abstract UAReturn Execute(string CommandScript);

        public abstract object ExecuteR(ServerCommand Command, bool hasReturn, bool hasUAReturn);

        public abstract UAReturn DatasetSaveas(ServerDataSource dataSource);//Anil 
        
        public abstract UAReturn CloseDataset(ServerDataSource dataSource);//Anil 

        #region variablegrid 
        public abstract UAReturn editDatasetVarGrid(ServerDataSource dataSource, string colName, string colProp, string newValue, List<string> colLevels);//var grid

        public abstract UAReturn makeColumnFactor(ServerDataSource dataSource, string colName);//var grid

        public abstract UAReturn addNewColDatagrid(string colName, string dgridval, int rowindex, ServerDataSource dataSource);//add row in vargrid and col in datagrid

        public abstract UAReturn removeVarGridCol(string colName, ServerDataSource dataSource);//remove row from variable grid

        public abstract UAReturn changeColLevels(string colName, List<ValLvlListItem> finalLevelList, ServerDataSource dataSource);//change levels

        public abstract UAReturn addColLevels(string colName, List<string> finalLevelList, ServerDataSource dataSource);//add levels

        public abstract UAReturn changeMissing(string colName, string colProp, List<string> newMisVal, string mistype, ServerDataSource dataSource);//change levels

        public abstract object getColNumFactors(string colName, ServerDataSource dataSource);

        public abstract UAReturn setScaleToNominalOrOrdinal(string colName, List<FactorMap> fmap, string changeTo, ServerDataSource dataSource);

        public abstract List<FactorMap> getColFactormap(string colName, ServerDataSource dataSource);

        public abstract UAReturn setNominalOrOrdinalToScale(string colName, List<FactorMap> fmap, string changeTo, ServerDataSource dataSource);
        #endregion

        #region datagrid
        public abstract UAReturn editDatagridCell(string colName, string celdata, int rowindex, ServerDataSource dataSource);//edit existing data row

        public abstract UAReturn addNewDataRow(string colName, string celdata, string rowdata, int rowindex, ServerDataSource dataSource);//add new data row

        public abstract UAReturn removeDatagridRow( int rowindex, ServerDataSource dataSource);//remove data row
        #endregion

        #region Package related
        public abstract void LoadDefPacakges();//30Mar2015
        public abstract UAReturn ShowInstalledPackages();
        public abstract UAReturn ShowLoadedPackages();

        public abstract UAReturn InstallLocalPackage(string[] pkgfilenames, bool autoLoad = true, bool overwrite = false);//(string package, string filepath);
        public abstract UAReturn InstallCRANPackage(string packagename);
        public abstract UAReturn InstallReqPackageFromCRAN(string packagename);//27Aug2015 //installs the required packge calling one function from BlueSky R pacakge
        public abstract UAReturn setCRANMirror();

        public abstract UAReturn LoadLocalPackage(string package);
        public abstract UAReturn LoadPackageFromList(string[] packagenames);

        public abstract UAReturn UnloadPackages(string[] packagenames);
        public abstract UAReturn UninstallPackages(string[] packagenames);
        #endregion
    }
}
