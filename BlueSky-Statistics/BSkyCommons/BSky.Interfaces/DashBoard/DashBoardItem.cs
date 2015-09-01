using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace BSky.Interfaces.DashBoard
{
    // Holds all this attributes(properties) those are needed while creating MenuItem
    public class DashBoardItem
    {
        public String Name { get; set; }
        public bool isGroup { get; set; } // its like "is this item a parent node item". isGroup = True means its parent.
        public ICommand Command { get; set; } // run this stored command on clicking menu item.
        public object CommandParameter { get; set; }
        public List<DashBoardItem> Items { get; set; } //if isGroup=true then this holds the submenu items.
        public string iconfullpathfilename { get; set; } //11Jun2015 set icon for menuitem
        public bool showshortcuticon { get; set; } //11Jun2015 show/hide icon in toolbar
    }
}
