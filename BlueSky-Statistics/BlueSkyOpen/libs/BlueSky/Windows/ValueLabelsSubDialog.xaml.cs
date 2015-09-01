using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BSky.Statistics.Common;

namespace BlueSky.Windows
{
    /// <summary>
    /// Interaction logic for ValueLabelsSubDialog.xaml
    /// </summary>
    /// 
        
    public partial class ValueLabelsSubDialog : Window
    {
        public bool OKclicked;
        
        private List<FactorMap> _factormap;
       
        public List<FactorMap> factormap
        {
            get
            {
                return _factormap;
            }
            set
            {
                _factormap = value;
            }
        }

        public ValueLabelsSubDialog()
        {
            InitializeComponent();
            createList();
        }

        public ValueLabelsSubDialog( List<FactorMap> flist, string firstHeader, string secondHeader)
        {
            InitializeComponent();
            //if(firstHeader.Equals("Level Names")) // reverse flist
            //{

            //}

            factormap = flist;
            listHeader1.Content = firstHeader;
            listHeader2.Content = secondHeader;
            createList();
        }
       
        public void createList()
        {
            Listbox.ItemsSource = factormap; 
            Listbox.Width = 400;
            KeyboardNavigation.SetTabNavigation(Listbox, KeyboardNavigationMode.Cycle);

        }

        private void ok_button_Click(object sender, RoutedEventArgs e)
        {
            if (!isTextChanged) // if text is not changed
            {
                ValueLabelsSubDialog.GetWindow(this).Close();
            }
            else
            {
                if (factormap != null && factormap.Count < 1)//if list is empty. just close the dialog
                {
                    ValueLabelsSubDialog.GetWindow(this).Close();
                    return;
                }
                bool isEmpty = false;
                bool isDuplicate = false;
                int i = 0;
                int len = factormap.Count;

                foreach (FactorMap m in factormap)
                {
                    string s = m.labels + ":" + m.textbox;
                    //MessageBox.Show(s);

                    ////checking duplicates ////
                    i++;
                    for (int j = i; j < len; j++)
                    {
                        if ((m.textbox.Trim().Length > 0) && (m.textbox == factormap.ElementAt(j).textbox.Trim()))//blank fields should not be checked
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    ////checking empty ///
                    if (m.textbox.Trim().Length == 0)
                    {
                        isEmpty = true;
                    }

                }
                OKclicked = true;
                if (isDuplicate)
                {
                    MessageBox.Show("Duplicate Level Names are not allowed.", "Error! Duplicate not allowed.", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (isEmpty)
                {
                    if (MessageBox.Show("Empty values not allowed.", "Warning.", MessageBoxButton.OK, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        //restore cells with blank values
                        foreach (FactorMap m in factormap)
                        {
                            if (m.textbox == null || m.textbox.Trim().Length == 0)
                            {
                                m.textbox = m.labels;
                            }
                        }
                        Listbox.ItemsSource = null;
                        Listbox.ItemsSource = factormap;
                        return;
                    }
                    else if (MessageBox.Show("Empty factors will be converted to NAs.", "Warning.", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        ValueLabelsSubDialog.GetWindow(this).Close();
                    }
                    else
                    {
                        //try to put back original value that was replaced with space. 
                        //If there are multiple levels(fields) made as blank, it will be trickier to get all of them back in UI dialog.
                        foreach (FactorMap m in factormap)
                        {
                            if (m.textbox == null || m.textbox.Trim().Length == 0)
                            {
                                m.textbox = m.labels;
                            }
                        }
                        Listbox.ItemsSource = null;
                        Listbox.ItemsSource = factormap;
                    }
                }
                else
                    ValueLabelsSubDialog.GetWindow(this).Close();
            }
        }

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            ValueLabelsSubDialog.GetWindow(this).Close();
        }

        bool isTextChanged = false;
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            isTextChanged = true;
        }



    }

}
