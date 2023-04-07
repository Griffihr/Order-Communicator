using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Command_Transmission
{
    public partial class MainWindow : Window
    {

        private ObservableCollection<Command_Struct> CmdStrct = new ObservableCollection<Command_Struct>();

        public MainWindow()
        {
            InitializeComponent();

            //ObservableCollection<Command_Struct> CmdStrct = new ObservableCollection<Command_Struct>();

            CmdStrct.Add(new Command_Struct() {StartTs = 0, Prio = 1});

            DG1.ItemsSource = CmdStrct;
        }

        public class Command_Struct
        {
            public int MaxTid { get; set; }
            public int AntalUpdr { get; set; }
            public int MaxUpdrH { get; set; }
            public int UppAddr { get; set; }
            public int AvAddr { get; set; }
            public int UppdrAntal { get; set; }
            public int Prio { get; set; } 
            public int StartTs { get; set; }
            public int KördH { get; set; }

        }

        private void Add_button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
