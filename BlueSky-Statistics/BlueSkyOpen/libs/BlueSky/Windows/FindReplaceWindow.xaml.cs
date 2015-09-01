using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BlueSky.Windows
{
    /// <summary>
    /// Interaction logic for FindReplaceWindow.xaml
    /// </summary>
    public partial class FindReplaceWindow : Window
    {
        OutputWindow _ow;

        public FindReplaceWindow()
        {
            InitializeComponent();
            
        }

        public FindReplaceWindow(OutputWindow ow)
        {
            InitializeComponent();
            _ow = ow;
            findtxt.Focus();
        }

        private void findnextbutton_Click(object sender, RoutedEventArgs e)
        {
            if (_ow == null) return;

            string findtext = findtxt.Text != null ? findtxt.Text : string.Empty;
            _ow.FindText(findtext);
        }

        private void replacebutton_Click(object sender, RoutedEventArgs e)
        {
            if (_ow == null) return;
            bool foundanother = true;
            string replacetext = replacetxt.Text != null ? replacetxt.Text : string.Empty; ;

            string findtext = findtxt.Text != null ? findtxt.Text : string.Empty;

            foundanother = _ow.ReplaceWith(findtext, replacetext);
            if (!foundanother)
            {
                MessageBox.Show(this,"No more to replace.");
            }
        }

        private void replaceallbutton_Click(object sender, RoutedEventArgs e)
        {
            if (_ow == null) return;
            bool foundanother = true;
            string replacetext = replacetxt.Text != null ? replacetxt.Text : string.Empty; ;

            string findtext = findtxt.Text != null ? findtxt.Text : string.Empty;
            do
            {
                foundanother = _ow.ReplaceWith(findtext, replacetext);
            } while (foundanother);
            MessageBox.Show(this,"No more to replace.");
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            if (_ow == null) return;

            _ow.CloseFindReplace();
        }






    }
}
