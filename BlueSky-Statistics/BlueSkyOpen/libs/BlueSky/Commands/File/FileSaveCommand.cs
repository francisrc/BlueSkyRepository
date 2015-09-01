using System;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using System.Windows;
using BSky.Statistics.Common;
using BSky.Lifetime;
using BlueSky.CommandBase;
using BSky.Interfaces.Interfaces;

namespace BlueSky.Commands.File
{
    class FileSaveCommand : BSkyCommandBase
    {
        protected override void OnPreExecute(object param)
        {
        }

        public const String FileNameFilter = "Excel 2003 (*.xls)|*.xls " +
                                            "|Excel 2007-2010 (*.xlsx)|*.xlsx " +
                                            "|Comma Seperated (*.csv)|*.csv " +
                                            "|DBF (*.dbf)|*.dbf " +
                                            "|R Obj (*.RData)|*.RData";

        protected override void OnExecute(object param)
        {
            IUnityContainer container = LifetimeService.Instance.Container;
            IDataService service = container.Resolve<IDataService>();
            IUIController controller = container.Resolve<IUIController>();

            //Get current filetype from loaded dataset. This is file extension and Filter
            DataSource actds = controller.GetActiveDocument();//06Nov2012
            if (actds == null)
                return;
            string datasetName = "" + actds.Name;//uadatasets$lst$
            //string datasetName = "uadatasets$lst$"+controller.GetActiveDocument().Name;
            //Also try to get the filename of currently loaded file. This is FileName.
            string extension =  controller.GetActiveDocument().Extension.ToLower();
            string filename = controller.GetActiveDocument().FileName;
            string filter = null;
            switch (extension)
            {
                case "csv": filter = "Comma Seperated (*.csv)|*.csv"; break;
                case "xls": filter = "Excel 2003 (*.xls)|*.xls"; break;
                case "xlsx": filter = "Excel 2007-2010 (*.xlsx)|*.xlsx"; break;
                case "dbf": filter = "DBF (*.dbf)|*.dbf"; break;
                case "rdata": filter = "R Obj (*.RData)|*.RData"; break;
                default: filter = "All Files(*.*)|*.*"; break;
            }

            if (extension.Equals("sav"))//show save-as dialog if current loaded file is SPSS file.
            {
                SaveFileDialog saveasFileDialog = new SaveFileDialog();
                saveasFileDialog.Filter = FileNameFilter;
                Window1 appwin = LifetimeService.Instance.Container.Resolve<Window1>();
                bool? output = saveasFileDialog.ShowDialog(appwin);//Application.Current.MainWindow);
                if (output.HasValue && output.Value)
                {

                    service.SaveAs(saveasFileDialog.FileName, controller.GetActiveDocument());// #0
                    controller.GetActiveDocument().Changed = false;//21Mar2014 during close it should not prompt again for saving
                }
            }
            else
            {
                service.SaveAs(filename, controller.GetActiveDocument());// #0
                controller.GetActiveDocument().Changed = false;//21Mar2014 during close it should not prompt again for saving
            }

        }

        protected override void OnPostExecute(object param)
        {
        }

        ////Send executed command to output window. So, user will know what he executed
        //protected override void SendToOutputWindow(string command, string title)//13Dec2013
        //{
        //    #region Get Active output Window
        //    //////// Active output window ///////
        //    OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
        //    OutputWindow ow = owc.ActiveOutputWindow as OutputWindow; //get currently active window
        //    #endregion
        //    ow.AddMessage(command, title);
        //}
    }
}
