using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
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
using System.Windows.Threading;

namespace Command_Transmission
{
    public partial class MainWindow : Window
    {


        public ObservableCollection<Command_Struct> CmdStrct = new ObservableCollection<Command_Struct>();
        private DateTime startTime;

        public MainWindow()
        {
            InitializeComponent();

            CmdStrct.Add(new Command_Struct() { StartTs = 0, Prio = 1 });

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
            CmdStrct.Add(new Command_Struct() { StartTs = 0, Prio = 1 });
        }

        private void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpClient Client = new TcpClient();

                Int32 port = Convert.ToInt32(Port.Text);

                IPAddress Ip_Adr = System.Net.IPAddress.Parse(Convert.ToString(Ip_Adress.Text));

                IPEndPoint EndPoint = new IPEndPoint(Ip_Adr, port);

                Client.Connect(EndPoint);

                NetworkStream NetStream = Client.GetStream();

                StreamReader StrReader = new StreamReader(NetStream, Encoding.UTF8);
            }
            catch (ArgumentNullException en)
            {
                MessageBox.Show(en.Message);
            }

            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message);
            }

            catch (Exception ec)
            {
                MessageBox.Show(ec.Message);
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Timer.Text = (DateTime.Now - startTime).ToString(@"hh\:mm\:ss");
        }


        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            Main_Prog();
        }
        private void Main_Prog()
        {

            int StopE = 1, StopD = 1;
            
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += dispatcherTimer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1);
            startTime = DateTime.Now;
            timer.Start();

            while (StopD != 0)
            {



                if (StopE == 0)
                {
                    break;
                }
            }

        }
    }
}
