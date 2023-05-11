using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
        public DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            timer.Tick += dispatcherTimer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1);

            CmdStrct.Add(new Command_Struct() {Igång = true, StartTs = 1, Prio = 0 });

            DG1.ItemsSource = CmdStrct;

        }
        
        class ObservableObject : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }

        public class Command_Struct
        {
            int _antalUpdr;
            public bool Igång { get; set; }
            public int MaxTid { get; set; }           
            public int AntalUpdr
            {
                get { return _antalUpdr; }
                set { _antalUpdr = value; }
            }
            public int MaxUpdrH { get; set; }
            public int UppAddr { get; set; }
            public int AvAddr { get; set; }
            public int Prio { get; set; }
            public int StartTs { get; set; }
            public int KördH { get; set; }

        }

        private void Add_button_Click(object sender, RoutedEventArgs e)
        {
            CmdStrct.Add(new Command_Struct() {Igång = true, StartTs = 0, Prio = 1 });;
        }

        private async void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Xd");

                Int32 port = Convert.ToInt32(Port.Text);

                IPAddress Ip_Adr = System.Net.IPAddress.Parse(Convert.ToString(Ip_Adress.Text));

                IPEndPoint ipEndPoint = new IPEndPoint(Ip_Adr, port);

                using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                await client.ConnectAsync(ipEndPoint);
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
        public void Main_Prog()
        {
            int StopE = 1, StopD = 1, i = 1;           
            
            startTime = DateTime.Now;
            timer.Start();

            int CmdStrct_Lenght = CmdStrct.Count();
            EleCount.Text = "Amount =" + CmdStrct_Lenght;

            
            foreach (Command_Struct Cmd_Amount in CmdStrct)
            {
                


            }
        }
    }
}
