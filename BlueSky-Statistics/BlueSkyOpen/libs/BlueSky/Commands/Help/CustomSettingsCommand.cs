﻿using BlueSky.Windows;
using BlueSky.CommandBase;
using System.Windows;

namespace BlueSky.Commands.Help
{
    class CustomSettingsCommand : BSkyCommandBase
    {

        protected override void OnPreExecute(object param)
        {
            //throw new NotImplementedException();
        }

        protected override void OnExecute(object param)
        {
            //MessageBox.Show("Hurrrrey!!");
            CustomSettingsWindow customwin = new CustomSettingsWindow();
            customwin.Owner=(Application.Current.MainWindow);
            customwin.ShowDialog();
            customwin.Activate();
        }

        protected override void OnPostExecute(object param)
        {
            //throw new NotImplementedException();
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
