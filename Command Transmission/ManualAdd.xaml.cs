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
    /// <summary>
    /// Interaction logic for ManualAdd.xaml
    /// </summary>
    public partial class ManualAdd : Window
    {
        public ManualAdd()
        {

            InitializeComponent();

            Upp_Addr.Text = "0";
            Av_Addr.Text = "0";

            Start_Ts.Text = "1";
            Prio.Text = "0";
            P3.Text = "0";
            P4.Text = "0";
            P5.Text = "0";
            P6.Text = "0";
            P7.Text = "0";
            P8.Text = "0";
            P9.Text = "0";
            P10.Text = "0";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.Command_Struct _Command_Struct = new MainWindow.Command_Struct();
                _Command_Struct.UppAddr = int.Parse(Upp_Addr.Text);
                _Command_Struct.AvAddr = int.Parse(Av_Addr.Text);

                _Command_Struct.Prio = int.Parse(Prio.Text);
                _Command_Struct.StartTs = int.Parse(Start_Ts.Text);

                _Command_Struct.Param3 = int.Parse(P3.Text);
                _Command_Struct.Param4 = int.Parse(P4.Text);
                _Command_Struct.Param5 = int.Parse(P5.Text);
                _Command_Struct.Param6 = int.Parse(P6.Text);
                _Command_Struct.Param7 = int.Parse(P7.Text);
                _Command_Struct.Param8 = int.Parse(P8.Text);
                _Command_Struct.Param9 = int.Parse(P9.Text);
                _Command_Struct.Param10 = int.Parse(P10.Text);
                
                MainWindow.ModifyMessage.ManualSend(_Command_Struct);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
