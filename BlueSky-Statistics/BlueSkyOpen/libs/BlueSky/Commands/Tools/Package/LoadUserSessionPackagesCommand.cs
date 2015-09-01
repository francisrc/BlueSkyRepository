using System;
using System.Collections.Generic;
using BlueSky.CommandBase;
using BSky.Lifetime.Interfaces;
using BSky.Lifetime;
using BSky.Statistics.Common;
using System.Windows;

namespace BlueSky.Commands.Tools.Package
{
    class LoadUserSessionPackagesCommand : BSkyCommandBase
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012
        RecentItems userPackageList = LifetimeService.Instance.Container.Resolve<RecentItems>();//06Feb2014
        protected override void OnPreExecute(object param)
        {

        }

        protected override void OnExecute(object param)
        {
            try
            {
                //Get User Package names. 
                //NOTE :: Package must already be installed. This logic does not look for installing missing packages. It just loads.
                List<string> usrsesspkgs = userPackageList.RecentFileList;
                if (usrsesspkgs.Count > 0)//non-empty list
                {
                    string[] packagenames = new string[usrsesspkgs.Count];
                    int i = 0;
                    foreach (string pkgname in usrsesspkgs)
                    {
                        packagenames[i] = pkgname;
                        i++;
                    }




                    PackageHelperMethods phm = new PackageHelperMethods();
                    UAReturn r = phm.LoadPackageFromList(packagenames);// PackageFileLoad(packagename);
                    if (r != null)
                    {
                        if (r.Success)//all package got loaded
                        {
                            SendToOutputWindow("User Package(s)", r.CommandString);
                        }
                        else if (r.CommandString.Trim().Length > 0) // some got loaded some failed.
                        {
                            SendToOutputWindow("One or more User Session Package(s) failed to load.", r.CommandString);
                        }
                    }
                    else
                    {
                        SendToOutputWindow("Error Loading Package(s)", "");
                    }
                }
                else
                {
                    SendToOutputWindow("User Package(s)", "- User's Session List is Empty");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while loading user session package(s).", "Error Occurred!");
                logService.WriteToLogLevel("Error:", LogLevelEnum.Error, ex);
            }
        }

        protected override void OnPostExecute(object param)
        {

        }

    }
}
