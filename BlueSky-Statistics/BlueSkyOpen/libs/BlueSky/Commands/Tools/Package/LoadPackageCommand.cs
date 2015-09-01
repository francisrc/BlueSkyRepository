﻿using System;
using System.Windows;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using BSky.Statistics.Common;
using BlueSky.CommandBase;


namespace BlueSky.Commands.Tools.Package
{
    class LoadPackageCommand : BSkyCommandBase
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012
        protected override void OnPreExecute(object param)
        {

        }

        protected override void OnExecute(object param)
        {
            try
            {
                string packagename = Microsoft.VisualBasic.Interaction.InputBox("Enter package name that you want to load.\nNote: package must be already installed using 'install package' option.", "Load Library", "");
                if (string.IsNullOrEmpty(packagename))
                {
                    //MessageBox.Show("Title/Command cannot be empty, Exiting Dialog install", "Info: Dialog Title Empty.");
                    return;
                }

                PackageHelperMethods phm = new PackageHelperMethods();
                UAReturn r = phm.PackageFileLoad(packagename);// PackageFileLoad(packagename);
                if (r != null && r.Success)
                {
                    SendToOutputWindow( "Load Package", r.CommandString);
                }
                else
                {
                    SendToOutputWindow("Error Loading User Session Package.", "");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while loading package.", "Error Occurred!");
                logService.WriteToLogLevel("Error:", LogLevelEnum.Error, ex);
            }
        }

        protected override void OnPostExecute(object param)
        {

        }

        //public UAReturn PackageFileLoad(string filename)//06Dec2013 For loading package in R memory for use
        //{
        //    if (filename != null && filename.Length > 0)
        //    {
        //        IUnityContainer container = LifetimeService.Instance.Container;
        //        IDataService service = container.Resolve<IDataService>();

        //            string filepath = Path.GetDirectoryName(filename);
        //            string packagename = Path.GetFileName(filename);
        //            return service.loadPackage(packagename);


        //        //appwindow.RefreshRecent();
        //    }
        //    return null;
        //}

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
