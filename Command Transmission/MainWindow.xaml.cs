using Microsoft.Windows.Themes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Command_Transmission.MainWindow;

namespace Command_Transmission
{   
    public partial class MainWindow : Window
    {   
        public ObservableCollection<Command_Struct> CmdStrct = new ObservableCollection<Command_Struct>();
        private DateTime startTime;
        public DispatcherTimer timer = new DispatcherTimer();
        public int index;
        public TcpClient tcpClient = new TcpClient();
        public NetworkStream ns;
        public int b = 0;

        public class MyViewModel : ObservableObject
        {
            private ObservableCollection<Command_Struct> _CmdStrctList;
            public ObservableCollection<Command_Struct> CmdStrctList
            {
                get { return _CmdStrctList; }
                set { SetProperty(ref _CmdStrctList, value); }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            timer.Tick += dispatcherTimer_Tick;
            timer.Interval = TimeSpan.FromSeconds(1);

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Ip_Adress.Text = ip.ToString();
                }
            }

            Port.Text = "30001";

            CmdStrct.Add(new Command_Struct() {Igång = true, StartTs = 0, Prio = 1 });
            DG1.ItemsSource = CmdStrct;
           
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Timer.Text = (DateTime.Now - startTime).ToString(@"hh\:mm\:ss");
        }

        private void Add_button_Click(object sender, RoutedEventArgs e)
        {
            CmdStrct.Add(new Command_Struct() { Igång = true, StartTs = 0, Prio = 1 });
            byte[] messageArray = { 2 };
            byte Val1 = messageArray[0];
            byte Val2 = messageArray[1];

            string hVal1 = Val1.ToString();
            string hVal2 = Val2.ToString();

            string hVal = hVal1 + hVal2;

            Console.WriteLine(hVal1);

            int dVal = Convert.ToInt32(hVal, 16);

            Console.WriteLine(dVal);
        }

       
        public class ObservableObject : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
            {
                if (Equals(storage, value)) return false;
                storage = value;
                NotifyPropertyChanged(propertyName);
                return true;
            }
        }



        public void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IPAddress ipAdrs = IPAddress.Parse(Ip_Adress.Text);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAdrs, Convert.ToInt32(Port.Text));
                tcpClient.Connect(ipEndPoint);
                
                
                //NetworkStream ns = tcpClient.GetStream();
                
                
                MessageBox.Show("Connected to Manager");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }   
        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            if (tcpClient != null && tcpClient.Client.Connected)
            {
                startTime = DateTime.Now;
                timer.Start();
                index = 0;
                Task.Run(() => MainRead());
            }
            else
            {
                MessageBox.Show("Not connected to simulator");
                return;
            }
        }
        public void Initiate_Order()
        {
            byte[] aMessage = new byte[34];

            if (index > CmdStrct.Count) index = 0;

            Command_Struct command_struct = CmdStrct[index];
                
            if (command_struct.Igång == true)
            {               
                if (index > 100) index = 0;
                command_struct.index = index;

                MessageCreate(command_struct);

                ns.Write(aMessage, 0, aMessage.Length);

                index++;
            }
            return;
        }
        
        public async Task MainRead()
        {
            var aMessage = new byte[34];
                        
            while (true)
            {                
                ns = tcpClient.GetStream();
                
                if (ns.DataAvailable)
                {                   
                    MemoryStream ms = new MemoryStream();
                    Byte[] rMessage = new Byte[1024];
                    int bytesToRead;
                    
                    while ((bytesToRead = ns.Read(rMessage, 0 ,rMessage.Length)) > 0)
                    {
                        ms.Write(rMessage, 0, bytesToRead);
                    }

                    StreamReader sr = new StreamReader(ms);
                    string text1 = Encoding.ASCII.GetString(ms.ToArray());

                    string text2 = sr.ReadToEnd();
                    Console.Write(text1 + "\n" + text2);
                    
                    /*
                    byte[] messageArray = ms.ToArray();
                    byte Val1 = messageArray[0];
                    byte Val2 = messageArray[1];

                    string hVal1 = Val1.ToString();
                    string hVal2 = Val2.ToString();

                    string hVal = hVal1 + hVal2;

                    Console.WriteLine(hVal1);

                    int dVal = Convert.ToInt32(hVal, 16);

                    Console.WriteLine(dVal);
                    
                    switch(dVal)
                    {
                       
                    }
                    */
                }
                
                await Task.Run(() => Initiate_Order());                
                                   
            }
        }
        public class Command_Struct : ObservableObject
        {
            private int _index;
            public int index 
            { 
                get { return _index; }
                set { SetProperty(ref _index, value); }
            }
            public bool Igång { get; set; }
            public int MaxTid { get; set; }

            private int _AntalUpdr;
            public int AntalUpdr 
            {
                get { return _AntalUpdr; }
                set { SetProperty(ref _AntalUpdr, value); }
            }
            public int MaxUpdrH { get; set; }
            public int Prio { get; set; }
            public int StartTs { get; set; }
            public int KördH { get; set; }
            public int UppAddr { get; set; }
            public int AvAddr { get; set; }
            public int Param3 { get; set; }
            public int Param4 { get; set; }
            public int Param5 { get; set; }
            public int Param6 { get; set; }
            public int Param7 { get; set; }
            public int Param8 { get; set; }
            public int Param9 { get; set; }
            public int Param10 { get; set; }
        }      

        private byte[] MessageCreate(Command_Struct cmdStrct)
        {
            Byte b1 = 0x87; Byte b2 = 0xCD; // Header
            Byte b3 = 0x00; Byte b4 = 0x08; // Size of header 
            Byte b5 = 0x00; Byte b6 = 0x14; // Size of message  
            Byte b7 = 0x00; Byte b8 = 0x01; // function code
            Byte b9 = 0x00; Byte b10 = 0x71; // q-meddelande
            Byte b11 = 0x00; Byte b12 = 0x16; // Size of message

            Byte b13 = (byte)cmdStrct.StartTs;
            Byte b14 = (byte)cmdStrct.Prio;

            Byte b15 = (byte)(cmdStrct.UppAddr);     // Första 8 bitarna av Upphämtnings adressen sparas i b15
            Byte b16 = (byte)(cmdStrct.UppAddr >> 8); // Nästa 8 bitar blir flyttade 8 steg till höger och sparas i b16

            Byte b17 = (byte)(cmdStrct.AvAddr);      // Samma princip för avlämnings adress
            Byte b18 = (byte)(cmdStrct.AvAddr >> 8);

            Byte b19 = (byte)(cmdStrct.Param3); Byte b20 = (byte)(cmdStrct.Param3 >> 8);
            Byte b21 = (byte)(cmdStrct.Param4); Byte b22 = (byte)(cmdStrct.Param4 >> 8);
            Byte b23 = (byte)(cmdStrct.Param5); Byte b24 = (byte)(cmdStrct.Param5 >> 8);
            Byte b25 = (byte)(cmdStrct.Param6); Byte b26 = (byte)(cmdStrct.Param6 >> 8);
            Byte b27 = (byte)(cmdStrct.Param7); Byte b28 = (byte)(cmdStrct.Param7 >> 8);
            Byte b29 = (byte)(cmdStrct.Param8); Byte b30 = (byte)(cmdStrct.Param8 >> 8);
            Byte b31 = (byte)(cmdStrct.Param9); Byte b32 = (byte)(cmdStrct.Param9 >> 8);
            Byte b33 = (byte)(cmdStrct.Param10); Byte b34 = (byte)(cmdStrct.Param10 >> 8);

            Byte[] aMessage = { b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16, b17, b18, b19, b20, b21, b22, b23, b24, b25, b26, b27, b28, b29, b30, b31, b32, b33, b34 };

            return aMessage;
        }
    }
}
