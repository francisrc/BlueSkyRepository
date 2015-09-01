using System;
using System.Collections.Generic;
using BSky.Interfaces;
using BSky.Interfaces.DashBoard;
using System.Xml;
using System.Windows.Input;
using BlueSky.Commands.Analytics.TTest;
using BSky.Controls;
using BSky.Lifetime;
using BSky.Lifetime.Interfaces;
using System.IO;
using System.Windows;
using BSky.Interfaces.Services;

namespace BlueSky.Services
{

    class XmlDashBoardService : IDashBoardService
    {
        ILoggerService logService = LifetimeService.Instance.Container.Resolve<ILoggerService>();//17Dec2012
        #region IDashBoardService Members
        //23Apr2015 const string FileName = @"./Config/menu.xml";
        string FileName = string.Format(@"{0}menu.xml", BSkyAppData.BSkyDataDirConfigFwdSlash);//23Apr2015 

        XmlDocument document;
        public string XamlFile { get; set; } //06Mar2013 XAML dialog filename

        public string XmlFile { get; set; } //06Mar2013 XML output template filename

        public void Configure()
        {
            document = new XmlDocument();
            bool success = false;
            try
            {
                document.Load(FileName);
                success = true;
            }
            catch (XmlException xe)
            {
                MessageBox.Show("XmlException while reading menu.xml");
                logService.WriteToLogLevel("XmlException.\n" + xe.StackTrace, LogLevelEnum.Fatal);
            }
            catch (DirectoryNotFoundException dnfe)
            {
                MessageBox.Show("DirectoryNotFoundException while reading menu.xml");
                logService.WriteToLogLevel("DirectoryNotFoundException.\n" + dnfe.StackTrace, LogLevelEnum.Fatal);
            }
            catch (FileNotFoundException fnfx)
            {
                MessageBox.Show("FileNotFoundException while reading menu.xml");
                logService.WriteToLogLevel("FileNotFoundException.\n" + fnfx.StackTrace, LogLevelEnum.Fatal);
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception while reading menu.xml");
                logService.WriteToLogLevel("Exception.\n" + e.StackTrace, LogLevelEnum.Fatal);
            }

            if(success)
                InitializeLocalMenu(document);
            
        }
        
        private void InitializeLocalMenu(XmlDocument menuDocument)
        {
            foreach (XmlNode nd in menuDocument.SelectNodes("//menus/*"))
            {
                DashBoardItem item = CreateItem(nd);
                OnAddDashBoardItem(item);
            }
        }

        //21Jul2015 For creating toolbar dialog icons
        public List<DashBoardItem> GetDashBoardItems()//returns all DashBoardItems
        {
            List<DashBoardItem> alldashboardItems = new List<DashBoardItem>();
            document = new XmlDocument();
            bool success = false;
            try
            {
                document.Load(FileName);
                success = true;
            }
            catch (XmlException xe)
            {
                MessageBox.Show("XmlException while reading menu.xml");
                logService.WriteToLogLevel("XmlException.\n" + xe.StackTrace, LogLevelEnum.Fatal);
            }
            catch (DirectoryNotFoundException dnfe)
            {
                MessageBox.Show("DirectoryNotFoundException while reading menu.xml");
                logService.WriteToLogLevel("DirectoryNotFoundException.\n" + dnfe.StackTrace, LogLevelEnum.Fatal);
            }
            catch (FileNotFoundException fnfx)
            {
                MessageBox.Show("FileNotFoundException while reading menu.xml");
                logService.WriteToLogLevel("FileNotFoundException.\n" + fnfx.StackTrace, LogLevelEnum.Fatal);
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception while reading menu.xml");
                logService.WriteToLogLevel("Exception.\n" + e.StackTrace, LogLevelEnum.Fatal);
            }
            if (success)
            {
                foreach (XmlNode nd in document.SelectNodes("//menus/*"))
                {
                    DashBoardItem item = CreateItem(nd); 
                    alldashboardItems.Add(item);
                }
            }
            return alldashboardItems;
        }

        /// <summary>
        /// Val is target parent location. Title is new command name.commandFile is the XAML filename.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="Title"></param>
        /// <param name="commandFile"></param>
        /// <param name="forcePlace"></param>
        /// <returns></returns>
        public bool? SetElementLocaton(string val, string Title, string commandFile,bool forcePlace, string AboveBelowSibling)
        {
            if(string.IsNullOrEmpty(val))
                return null;
            string[] nodes = val.Split('>');

            ////reloading a latest document. Modified by Install dialog window ///
            document.Load(FileName);

            XmlElement newelement = document.CreateElement("menu");

            XmlAttribute attrib = document.CreateAttribute("id");
            attrib.Value = Title.Replace(' ', '_');
            newelement.Attributes.Append(attrib);

            attrib = document.CreateAttribute("text");
            attrib.Value = Title;
            newelement.Attributes.Append(attrib);

            attrib = document.CreateAttribute("commandtemplate");
            attrib.Value = commandFile;
            newelement.Attributes.Append(attrib);

            attrib = document.CreateAttribute("commandoutputformat");
            attrib.Value = commandFile.Replace("xaml","xml");//same filenames(dialog and out-template) but diff extensions
            newelement.Attributes.Append(attrib);

            attrib = document.CreateAttribute("owner");
            attrib.Value = "";//Newly Insalled commands are always leaf node
            newelement.Attributes.Append(attrib);

            attrib = document.CreateAttribute("nodetype");
            attrib.Value = "Leaf";//Newly Insalled commands are always leaf node
            newelement.Attributes.Append(attrib);

            XmlNode element = document.SelectSingleNode("//menus");

            foreach (string node in nodes)//Traverse to target parent, where new command should be added
            {
                if (node == "Root")
                    continue;
                if (element == null)
                    return null;
                element = element.SelectSingleNode("./menu[@text='" + node + "']");
            }
            if (element == null)//parent not found.
                return null;

            //if parent node or new node then add as child
            if (element.HasChildNodes || (element.Attributes["id"]!=null && element.Attributes["id"].Value == "new_id"))
            {
                XmlNode temp = element.SelectSingleNode("./menu[@text='" + Title + "']");
                if (temp == null || forcePlace)
                {
                    element.AppendChild(newelement);//Add as a last leaf node.(last sibling)
                }
                else
                    return false;
            }
            else /// add as sibling below target node.(target node is leaf node here)
            {
                XmlNode temp = element.ParentNode.SelectSingleNode("./menu[@text='" + Title + "']");
                if (temp == null || forcePlace)
                {
                    //if (AboveBelowSibling.Trim().Equals("Below"))
                        
                    if (AboveBelowSibling.Trim().Equals("Above"))
                        element.ParentNode.InsertBefore(newelement, element);
                    else if (AboveBelowSibling.Trim().Equals("Below"))
                        element.ParentNode.InsertAfter(newelement, element);//06Feb2013
                    else
                        element.AppendChild(newelement); // 'else' is not needed here. No harm keeping it. this can work for parent
                    // those are not owner="BSky" and not having id="new_id" and do not have any children. (if you try to overwrite
                    // "Data File" command )

                }
                else
                    return false;
            }
            //23Apr2015 document.Save(@"./Config/menu.xml");
            document.Save(string.Format(@"{0}Menu.xml", BSkyAppData.BSkyDataDirConfigFwdSlash));//23Apr2015
            return true;
        }

        private DashBoardItem CreateItem(XmlNode node)
        {
            DashBoardItem item = new DashBoardItem();

            item.Name = GetAttributeString(node, "text");
            item.isGroup = node.HasChildNodes;

            if (node.HasChildNodes)
            {
                item.Items = new List<DashBoardItem>();
                foreach (XmlNode child in node.ChildNodes)
                    item.Items.Add(CreateItem(child));
            }
            else
            {
                UAMenuCommand cmd = new UAMenuCommand();
                cmd.commandtype = GetAttributeString(node, "command");
                if (string.IsNullOrEmpty(cmd.commandtype))
                {
                    cmd.commandtype = typeof(AUAnalysisCommandBase).FullName;
                }
                cmd.commandtemplate = GetAttributeString(node, "commandtemplate");
                cmd.commandformat = GetAttributeString(node, "commandformat");
                cmd.commandoutputformat = GetAttributeString(node, "commandoutputformat");
                cmd.text = GetAttributeString(node, "text"); //04mar2013
                //cmd.id = GetAttributeString(node, "id"); //04mar2013
                //cmd.owner = GetAttributeString(node, "owner"); //04mar2013
                item.Command = CreateCommand(cmd);
                item.CommandParameter = cmd;

                #region 11Jun2015 Set icon fullpathfilename 
                item.iconfullpathfilename = GetAttributeString(node, "icon");
                string showicon = GetAttributeString(node, "showtoolbaricon"); 
                if(showicon==null || showicon.Trim().Length == 0 || !showicon.ToLower().Equals("true"))
                {
                    item.showshortcuticon = false;
                }
                else
                {
                    item.showshortcuticon = true;
                }
                #endregion
            }
            return item;
        }
 
        private ICommand CreateCommand(UAMenuCommand cmd)
        {
            Type commandTypeObject = null;
            ICommand command = null;

            try
            {
                commandTypeObject = Type.GetType(cmd.commandtype);
                command = (ICommand)Activator.CreateInstance(commandTypeObject);
            }
            catch
            {
                    //Create new command instance using default command dispatcher
                logService.WriteToLogLevel("Could not create command. "+cmd.commandformat, LogLevelEnum.Error);
            }

            return command;
        }

        private bool GetAttributeBool(XmlNode nd, string name)
        {
            bool result = false;

            bool.TryParse(GetAttributeString(nd, name), out result);

            return result;
        }

        private string GetAttributeString(XmlNode nd, string name)
        {
            XmlAttribute att = null;
            att = nd.Attributes[name];
            return (att != null) ? att.Value : string.Empty;
        }

        public event EventHandler<DashBoardEventArgs> AddDashBoardItem;

        protected virtual bool OnAddDashBoardItem(DashBoardItem item)
        {
            if (AddDashBoardItem != null)
            {
                DashBoardEventArgs args = new DashBoardEventArgs();
                args.DashBoardItem = item;
                AddDashBoardItem(this, args);
                return true;
            }
            else
            {
                return false;
            }
        }

        public string SelectLocation(ref string newcommandname,  ref string AboveBelowSibling)
        {
            MenuEditor editor = new MenuEditor(newcommandname);
            editor.LoadXml(FileName);
            editor.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            editor.Activate();
            editor.ShowDialog();
            
            if (editor.DialogResult.HasValue && editor.DialogResult.Value)
            {
                string str = editor.ElementLocation;
                newcommandname = editor.NewCommandName;
                AboveBelowSibling = (editor.NewCommandAboveBelowSibling != null)? editor.NewCommandAboveBelowSibling:string.Empty;//06Feb2013
                XamlFile = (editor.XamlFile!=null && editor.XamlFile.Length>0) ? editor.XamlFile:string.Empty; //06Mar2013
                XmlFile = (editor.XmlFile != null && editor.XmlFile.Length > 0) ? editor.XmlFile : string.Empty; //06Mar2013
                return str;
            }
            return string.Empty;
        }


        #endregion
    }
}
