﻿#pragma checksum "..\..\..\Windows\AddFactorLevelsDialog.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "304CD6BC14CD148E95A181B1842F72B9"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace BlueSky.Windows {
    
    
    /// <summary>
    /// AddFactorLevelsDialog
    /// </summary>
    public partial class AddFactorLevelsDialog : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 18 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock textBlock1;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox faclvltxt;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock textBlock2;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox levelsListBox;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button addlvlButton;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button okbutton;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button cancelbutton;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button removelvlbutton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/BlueSky;component/windows/addfactorlevelsdialog.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.textBlock1 = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 2:
            this.faclvltxt = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this.textBlock2 = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 4:
            this.levelsListBox = ((System.Windows.Controls.ListBox)(target));
            return;
            case 5:
            this.addlvlButton = ((System.Windows.Controls.Button)(target));
            
            #line 23 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
            this.addlvlButton.Click += new System.Windows.RoutedEventHandler(this.addlvlButton_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.okbutton = ((System.Windows.Controls.Button)(target));
            
            #line 25 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
            this.okbutton.Click += new System.Windows.RoutedEventHandler(this.okbutton_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.cancelbutton = ((System.Windows.Controls.Button)(target));
            
            #line 26 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
            this.cancelbutton.Click += new System.Windows.RoutedEventHandler(this.cancelbutton_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.removelvlbutton = ((System.Windows.Controls.Button)(target));
            
            #line 28 "..\..\..\Windows\AddFactorLevelsDialog.xaml"
            this.removelvlbutton.Click += new System.Windows.RoutedEventHandler(this.removelvlbutton_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

