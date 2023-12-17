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
using System.Timers;
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
        private static System.Timers.Timer? pTimer;
        public static Stopwatch pWatch = new Stopwatch();
        private bool poll;

        private static void setTimer()
        {
            pTimer = new System.Timers.Timer(1000);
            pTimer.Elapsed += timerUpdateUi;
            pTimer.Enabled = true;
        }

        private static void timerUpdateUi(Object source, ElapsedEventArgs e)
        {

        }
       
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

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Ip_Adress.Text = ip.ToString();
                }
            }

            Port.Text = "30001";

            CmdStrct.Add(new Command_Struct() {Igång = true, StartTs = 1, Prio = 1 });
            DG1.ItemsSource = CmdStrct;
           
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Timer.Text = (DateTime.Now - startTime).ToString(@"hh\:mm\:ss");
        }

        private void Add_button_Click(object sender, RoutedEventArgs e)
        {
            CmdStrct.Add(new Command_Struct() { Igång = true, StartTs = 1, Prio = 1 });
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
                if (Equals(storage, value))
                {
                    return false;
                }
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
                      
                MessageBox.Show("Connected to Manager");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {

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
            var ns = tcpClient.GetStream();
            index = 1;     

            foreach (Command_Struct _Command_Struct in CmdStrct)
            {
                
                if (_Command_Struct.MaxUpdrH != 0 && _Command_Struct.Igång == false) 
                {
                    TimeSpan tsPerOrder = TimeSpan.FromHours(1) / _Command_Struct.MaxUpdrH;
                    TimeSpan tsLastOrder = DateTime.Now.Subtract(_Command_Struct.lastOrder);

                    if (tsLastOrder > tsPerOrder)
                    {
                        _Command_Struct.Igång = true;
                    }
                }

                if (_Command_Struct.Igång == true) // Checkar att uppdraget ska köra, skapar meddelandet och skickar till System managern.
                {
                    _Command_Struct.Igång = false;
                    _Command_Struct.index = index;
                    aMessage = qMessageCreate(_Command_Struct);

                    ns.Write(aMessage, 0, aMessage.Length);
                    poll = false;

                    return;
                }
                
                index++;
            }

            poll = true;

            return;
        }

        public async Task MainRead()
        {

            var ns = tcpClient.GetStream();
            var ms = new MemoryStream();
            DateTime pollTime = DateTime.Now;
            TimeSpan secSpan = TimeSpan.FromSeconds(1);

            ns.Flush();

            await Task.Run(() => Initiate_Order());

            byte[] rMessage = new byte[256];

            while (true)
            {

                if (poll == true && DateTime.Now.Subtract(pollTime) > secSpan)
                {
                    await Task.Run(() => Initiate_Order());
                    pollTime = DateTime.Now;
                }

                if (ns.DataAvailable) 
                {

                    while (rMessage[0] != 135 && ns.DataAvailable)
                    {
                        ns.Read(rMessage, 0, 1);
                    }

                    int bytesread = ns.Read(rMessage, 1, 5);

                    int bytesToRead = BitConverter.ToInt16(rMessage, 5);

                    bytesread = ns.Read(rMessage, 6, bytesToRead);
                   
                    int mVal = BitConverter.ToInt16(rMessage, 9);
                    int mIndex = BitConverter.ToInt16(rMessage, 12);
                    
                    if (mVal == 98)
                    {

                        if (rMessage[15] == 03)
                        {
                            Dispatcher.Invoke(void() => Text_Out.AppendText("Order" + mIndex + "Finished \r\n"));

                            TimeSpan orderTime = DateTime.Now.Subtract(pollTime);

                            foreach (Command_Struct _Command_Struct in CmdStrct)
                            {
                                if (_Command_Struct.mIndex == mIndex)
                                {
                                    _Command_Struct.mIndex = 0;
                                    _Command_Struct.index = 0;
                                    _Command_Struct.calcAverage(orderTime);
                                    _Command_Struct.lastOrder = DateTime.Now;

                                }
                            }
                        }


                        else if (rMessage[15] == 01)
                        {

                            Text_Out.AppendText("Order Acknowledged \r\n");

                            foreach (Command_Struct _Command_Struct in CmdStrct)
                            {
                                if (_Command_Struct.index == index)
                                {
                                    _Command_Struct.mIndex = mIndex;
                                }
                            }

                            await Task.Run(() => Initiate_Order());
                        }
               
                        else if (rMessage[15] == 00)
                        {
                            Dispatcher.Invoke(void () => Text_Out.AppendText("Order" + mIndex + "Rejected \r\n"));

                            await Task.Run(() => Initiate_Order());
                        }                      
                    }
                    
                    else if (mVal == 98)
                    {

                    }
                    Array.Clear(rMessage, 0, rMessage.Length);
                }
                
            }
        }
      
        public class Command_Struct : ObservableObject
        {
            public int ordersDone { get; set; } 
            public void calcAverage(TimeSpan time)
            {
                ordersDone += 1;
                avgTimeTotal.Add(time);
                avgTime = avgTimeTotal.Divide(ordersDone);
            }
            private int _index; // Index för att länka lokalt och System manager uppdrag.
            public int index 
            { 
                get { return _index; }
                set { SetProperty(ref _index, value); }
            }
            public int mIndex; // Index från System manager
            public bool Igång { get; set; }
            public TimeSpan avgTimeTotal { get; set; }
            public TimeSpan avgTime { get; set; }
            public DateTime lastOrder { get; set; } //Datetime för föregående uppdrag.
            public int MaxTid { get; set; }
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

        private byte[] nMessageCreate(Command_Struct cmdstrct)
        {
            Byte b1 = 0x87; Byte b2 = 0xCD; // Header
            Byte b3 = 0x00; Byte b4 = 0x08; // Size of header 
            Byte b5 = 0x00; Byte b6 = 0x06; // Size of Message
            Byte b7 = 0x00; Byte b8 = 0x01; // Function code
            Byte b9 = 0x00; Byte b10 = 0x6E; // Message Type
            Byte b11 = 0x00; Byte b12 = 0x02; // Number of params

            byte[] ordIndex = BitConverter.GetBytes(cmdstrct.mIndex);

            Byte b13 = ordIndex[0];
            Byte b14 = ordIndex[1];

            Byte[] nMessage = {b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14};
            return nMessage;
        }
        private byte[] qMessageCreate(Command_Struct cmdStrct)
        {
            Byte b1 = 0x87; Byte b2 = 0xCD; // Header
            Byte b3 = 0x00; Byte b4 = 0x08; // Size of header 
            Byte b5 = 0x00; Byte b6 = 0x16; // Size of message
            Byte b7 = 0x00; Byte b8 = 0x01; // function code
            Byte b9 = 0x00; Byte b10 = 0x71; // q-meddelande
            Byte b11 = 0x00; Byte b12 = 0x0C; // Antal Parametrar

            Byte b13 = (byte)cmdStrct.StartTs;
            Byte b14 = (byte)cmdStrct.Prio;

            byte[] UppAddr = BitConverter.GetBytes(cmdStrct.UppAddr);

            Byte b15 = UppAddr[1];
            Byte b16 = UppAddr[0];

            byte[] AvAddr = BitConverter.GetBytes(cmdStrct.AvAddr);

            Byte b17 = AvAddr[1];
            Byte b18 = AvAddr[0];

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
