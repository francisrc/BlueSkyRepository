using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BSky.Lifetime;
using BSky.Interfaces.Interfaces;

namespace BlueSky
{
    class OutputWindowContainer : IOutputWindowContainer
    {
        /// <summary>
        /// The container of all the output windows.
        /// </summary>
        Dictionary<string, IOutputWindow> outputlist = new Dictionary<string, IOutputWindow>();

        void OutPutWindowContainer()
        {
        }


        IOutputWindow activeoutputwindow;// has the reference of currently active window
        public IOutputWindow ActiveOutputWindow /// during Resolve<IOutputWindowContainer>, this property is called automatically.
        {
            get 
            {
                if (outputlist.Count == 0)// if there are no outputwindow then create one.
                {
                    IOutputWindow iow = new OutputWindow();// output window created
                    AddOutputWindow(iow);/// default (or First) output window added to container
                    activeoutputwindow = iow;//making that first window as the active window, for poulating output
                }
                SetLicenseInfo();//27Oct2014 For refreshing Licensing info each time output is sent to output window.
                return activeoutputwindow; // returning the reference of currently acvtive output window
            }
            set 
            {
                activeoutputwindow = value; // setting some window as active
            }
        }

        int count;
        public int Count
        {
            get 
            {
                count = outputlist.Count;
                return count;
            }
        }

        int wincount=0;//for naming windows. This can only increase.
        public int WinCount
        {
            get { return wincount; }
        }

        public Dictionary<string, IOutputWindow> AllOutputWindows//05Feb2013
        {
            get { return outputlist; }
        }

        #region IOutputWindowContainer members

        //Adding new output window to the container by provinding the reference of that output window.
        //This method will name that window automatically.
        public void AddOutputWindow(IOutputWindow iow)
        {
            wincount++;
            iow.WindowName = "Ouput and Syntax Window-" + wincount.ToString(); 
            outputlist.Add(iow.WindowName, iow);
            SetActiveOuputWindow(iow.WindowName);
            ////////
            //if (outputlist.Count > 1)
            //{
                MainWindow mwindow = LifetimeService.Instance.Container.Resolve<MainWindow>();//28Jan2013
                Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();
                window.OMH.AddOutputMenuItem(iow.WindowName);/// adding window name to outputmenu. And putting check 
                 //Do same for window menu when you create that menu

                Window temp = iow as Window;
                temp.Height = 650;
                temp.Width = 840;

                temp.Owner = mwindow;// Main Window invisible one is parent and not the app-window that has menus 'File' ...

                temp.Show();
            //}
        }

        // Removing output window from the container that contains all the outputwindows, by providing windowname
        public void RemoveOutputWindow(string Windowname)
        {
            if (outputlist.ContainsKey(Windowname))
            {
                outputlist.Remove(Windowname);

                Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();
                window.OMH.RemoveOutputMenuItem(Windowname);//remove from Output menu And Window menu.

                //set the last window in sequence as a active window.
                if (outputlist.Count > 0)
                {
                    SetActiveOuputWindow(outputlist.ElementAt(outputlist.Count - 1).Value.WindowName);
                    ////putting check on another item in menu
                    window.OMH.CheckOutputMenuItem(outputlist.ElementAt(outputlist.Count - 1).Value.WindowName);
                }
            }
        }

        // Setting output window as active window for populating output, by providing its name //
        public void SetActiveOuputWindow(string Windowname)
        {
            //string WinName = Windowname.Replace("(Active)", "").Trim();
            if (outputlist.ContainsKey(Windowname))
            {
                outputlist.TryGetValue(Windowname, out activeoutputwindow);//get ref of output window
                ///Defaulting title of all windows ////
                foreach(KeyValuePair<String,IOutputWindow> itm in outputlist)
                {
                    Window tempow = itm.Value as Window; 
                    tempow.Title = itm.Key;///Key is WindowName
                }
                // Add only (Active) to only one output window
                (activeoutputwindow as Window).Title = Windowname + " (Active)";
            }
            else
                activeoutputwindow = null;
        }

        // Get output window reference whose name is provided.
        public IOutputWindow GetOuputWindow(string Windowname)
        {
            IOutputWindow iow = null;
            outputlist.TryGetValue(Windowname, out iow);
            return iow;
        }
        #endregion


        #region License Info
        private void SetLicenseInfo()
        {
  
        }
        #endregion
    }
}
