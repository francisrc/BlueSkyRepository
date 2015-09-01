using BSky.Statistics.Common;

namespace BSky.Interfaces.Interfaces
{
    public interface IDataService
    {
        DataSource NewDataset();//03Jan2014
        DataSource Open(string filename, string sheetname);
        DataSource OpenDataframe(string dframename, string sheetname); //13Feb2014
        object GetOdbcTableList(string filename);//27Jan2014
        DataSource Refresh(DataSource dsname);//25Mar2013
        void SaveAs(string filname, DataSource ds);//Anil
        void Close(DataSource ds);
        bool isDatasetNew(string dsname);//17Feb2014

        //06Dec2013
        #region Package Related
        UAReturn installPackage(string[] pkgfilenames, bool autoLoad = true, bool overwrite = false);//(string package, string filepath);
        UAReturn installCRANPackage(string packagename);
        UAReturn installReqPackageCRAN(string packagename);//27Aug2015
        UAReturn setCRANMirror();


        UAReturn loadPackage(string package);
        UAReturn loadPackageFromList(string[] packagenames);

        UAReturn showInstalledPackages();
        UAReturn showLoadedPackages();

        UAReturn unloadPackage(string[] packagenames);
        UAReturn uninstallPackage(string[] packagenames);

        #endregion
    }

}
