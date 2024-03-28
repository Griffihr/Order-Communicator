using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Command_Transmission
{
    public partial class NameWindow : Window
    {
        private static string NewName;
        public NameWindow()
        {
            InitializeComponent();     
        }

        public static string GetName(string oldname)
        {
            NameWindow nameWindow = new NameWindow();

            NewName = oldname;

            nameWindow.OldName.Text = oldname;

            nameWindow.NameBox.Focus();

            nameWindow.ShowDialog();

            return NewName;
        }

        public void setName(string name)
        {
            OldName.Text = name;
        }


        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            NewName = this.NameBox.Text;
            this.Close();

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
