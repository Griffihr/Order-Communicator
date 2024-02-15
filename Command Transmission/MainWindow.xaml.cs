using Microsoft.Windows.Themes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
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
using static System.Net.Mime.MediaTypeNames;
using static Command_Transmission.ViewListWindow;

namespace Command_Transmission
{   
    public partial class MainWindow : Window
    {   
        public static ObservableCollection<Command_Struct> CmdStrct = new ObservableCollection<Command_Struct>();
        private DateTime startTime;
        public TcpClient tcpClient = new TcpClient();
        public static Stopwatch pWatch = new Stopwatch();
        private bool poll;
        private bool mainProgRun;
        private int LastOrderMindex;
        private int CurrentOrderMindex = 0;
        private int LastOrderPrio = 228;
        public System.Timers.Timer _Timer;
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

            setTimer();

            Port.Text = "30001";
            CmdStrct.Add(new Command_Struct() { Enabled = true, Run = true, StartTs = 1, Prio = 0 , TimeRun = true});
            
            DG1.ItemsSource = CmdStrct;
            this.DataContext = this;
           
        }

        private void setTimer()
        {
            _Timer = new System.Timers.Timer(1000);
            _Timer.Elapsed += OnTimedEvent;
            _Timer.AutoReset = true;
        } 
        public class ObservableObject : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Add_button_Click(object sender, RoutedEventArgs e)
        {
            if (mainProgRun == true)
            {
                CmdStrct.Add(new Command_Struct() { Enabled = false, Run = true, StartTs = 1, Prio = 0, TimeRun = true});
            }
            else
            {
                CmdStrct.Add(new Command_Struct() { Enabled = true, Run = true, StartTs = 1, Prio = 0, TimeRun = true});
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
        private async void Start_Button_Click(object sender, RoutedEventArgs e)
        {

            if (tcpClient != null && tcpClient.Client.Connected)
            {
                startTime = DateTime.Now;
                mainProgRun = true;
                await Task.Run(() => MainRead());
            }
            else
            {
                MessageBox.Show("Not connected to simulator");
                return;
            }

        }
        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            var CancelmIndexList = new List<int>();
            foreach (var CancelIndex in DG1.SelectedItems)
            {
                int index = DG1.Items.IndexOf(CancelIndex);
                CmdStrct[index].Enabled = false;
                CancelmIndexList.Add(CmdStrct[index].MIndex);
            }

            CancelTasks(CancelmIndexList);
        }
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            Text_Out.AppendText("Program stopped");
            Text_Out.CaretIndex = Text_Out.Text.Length;
            Text_Out.ScrollToEnd();
            mainProgRun = false;
        }

        public void OnTimedEvent( Object State, ElapsedEventArgs e)
        {
            Initiate_Order();
        }


        public void MainRead()
        {            
            var ns = tcpClient.GetStream();
            var clearBuffer = new byte[4096];
            while (ns.DataAvailable)
            {
                ns.Read(clearBuffer, 0 , clearBuffer.Length);
            }


            DateTime pollTime = DateTime.Now;
            TimeSpan secSpan = TimeSpan.FromSeconds(1);

            Initiate_Order();

            while (mainProgRun == true)
            {

                if (ns.DataAvailable)
                {
                    var rMessage = ReadAsync();

                    if (rMessage[0] == 135) 
                    { 
                        Byte[] mValByte = { rMessage[9], rMessage[8] };
                        Byte[] mIndexByte = { rMessage[13], rMessage[12] };

                        int mVal = BitConverter.ToInt16(mValByte);
                        int mIndex = BitConverter.ToInt16(mIndexByte);

                        if (mVal == 98)
                        {
                            BMessage(rMessage[15], mIndex);


                            Initiate_Order();
                        }

                        else if (mVal == 115)
                        {
                            Byte[] bArray = { rMessage[17], rMessage[16] };
                            short bType = BitConverter.ToInt16(bArray, 0);

                            SMessage(bType, mIndex);
                        }

                        Array.Clear(rMessage, 0, rMessage.Length);
 
                    }
                }
            }

            return;
        }
        public byte[] ReadAsync()
        {
            var rMessage = new byte[60];
            var ns = tcpClient.GetStream();
            while (ns.DataAvailable)
            { 
                if (rMessage[0] == 135)
                {

                    int bytesread = ns.Read(rMessage, 1, 5);

                    int bytesToRead = BitConverter.ToInt16(rMessage, 5);

                    bytesread = ns.Read(rMessage, 6, bytesToRead);

                    return rMessage;

                }
                else
                {
                    ns.Read(rMessage, 0, 1);
                }
            }

            return rMessage;
        }

        public void Initiate_Order()
        {                    
            int index = 1;
            
            foreach (Command_Struct _Command_Struct in CmdStrct)
            {

                if (_Command_Struct.MaxUpdrH != 0 && _Command_Struct.Enabled == true)
                {
                    TimeSpan tsPerOrder = TimeSpan.FromHours(1) / _Command_Struct.MaxUpdrH;
                    TimeSpan tsLastOrder = DateTime.Now.Subtract(_Command_Struct.OrderStartTime);



                    if (TimeSpan.Compare(tsLastOrder, tsPerOrder) == 1 && _Command_Struct.TimeRun == true && CurrentOrderMindex != LastOrderMindex)
                    {
                        _Command_Struct.Run = true;
                        _Command_Struct.TimeRun = false;
                        _Timer.Stop();
                    }
                    else if (TimeSpan.Compare(tsLastOrder, tsPerOrder) <= 0)
                    {
                        _Command_Struct.Run = false;
                    }
                }

                if (_Command_Struct.Run == true && _Command_Struct.Enabled == true) // Checkar att uppdraget ska köra, skapar meddelandet och skickar till System managern.
                {

                    _Command_Struct.Run = false;
                    _Command_Struct.Index = index;

                    byte[] aMessage = new byte[34];
                    aMessage = qMessageCreate(_Command_Struct);

                    var ns = tcpClient.GetStream();
                    ns.Write(aMessage, 0, aMessage.Length);

                    return;

                }

                index++;

            }

            _Timer.Start();

            return;
        }

        public void TextBoxAppend(string text)
        {
            Text_Out.AppendText(text);
            Text_Out.CaretIndex = Text_Out.Text.Length;
            Text_Out.ScrollToEnd();
            return;
        }

        public void CancelTasks(List<int> CancelmIndex)
        {

            try
            {
                var ns = tcpClient.GetStream();

                foreach (var _CancelmIndex in CancelmIndex)
                {

                    ViewList.Remove(ViewList.FirstOrDefault(x => x.MIndex == _CancelmIndex));

                    foreach (Command_Struct _Command_Struct in CmdStrct)
                    {
                        if (_Command_Struct.MIndex == _CancelmIndex)
                        {
                            _Command_Struct.Enabled = false;
                            var nMessage = nMessageCreate(_Command_Struct);
                            ns.Write(nMessage, 0, nMessage.Length);
                        }
                    }


                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            

            return;
        }



        public void SMessage(int type, int mIndex)
        {
            switch (type)
            {
                case 4096: //Order ready to pick up
                    {
                        Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + " Ready to pick upp \r\n"));
                        break;
                    }



                case 4097: //Order Loaded
                    {
                        foreach (Command_Struct _Command_Struct in CmdStrct)
                        {
                            if (_Command_Struct.MIndex == mIndex)
                            {
                                
                                _Command_Struct.OrderStartTime = DateTime.Now;

                                pWatch.Start();

                                CurrentOrderMindex = mIndex;
                             
                                Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + " Picked up \r\n"));                          
                                
                            }
                        }
                        break;
                    }
            
            }
        }

        public void BMessage(int type, int mIndex)
        {
            switch (type)
            {
                case 00: // Order Rejected

                    Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + " Rejected \r\n"));
                    break;

                case 01: // Order Acknowledged

                    int AckInd = 1;

                    foreach (Command_Struct _Command_Struct in CmdStrct)
                    {

                        if (_Command_Struct.Index == AckInd && _Command_Struct.MIndex == 0)
                        {
                            _Command_Struct.MIndex = mIndex;

                            VisualCommand_Struct _VisualCommand_Struct = ViewListWindow.ViewListStruct(_Command_Struct);

                            Dispatcher.Invoke(() => ViewListWindow.ViewList.Add(_VisualCommand_Struct));

                            Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + " Acknowledged \r\n"));

                            //bool test = _Command_Struct.Prio <= LastOrderPrio || LastOrderPrio == 228; //Testar om Prio värdet på meddelandet är samma eller lägre än föregående sista meddelandes eller om det inte finns något värde sparat.

                            if (_Command_Struct.Prio <= LastOrderPrio && _Command_Struct.MaxUpdrH == 0) {
                                LastOrderMindex = mIndex;
                                LastOrderPrio = _Command_Struct.Prio;
                            }

                            return;
                        }

                        AckInd++;

                    }

                    break;

                case 03: // Order Finished

                    pWatch.Stop();
                    TimeSpan ts = pWatch.Elapsed;
                    pWatch.Reset();

                    int i = 0;

                    foreach (Command_Struct _Command_Struct in CmdStrct)
                    {                   
                        if (_Command_Struct.MIndex == mIndex)
                        {
                            _Command_Struct.calcAverage(ts);

                            _Command_Struct.TimeRun = true;

                            VisualCommand_Struct _VisualCommand_Struct = ViewListWindow.ViewListStruct(_Command_Struct);

                            Dispatcher.Invoke(() => ViewListWindow.ViewList.Remove(_VisualCommand_Struct));

                            if (_Command_Struct.MaxUpdrH != 0)
                            {
                                _Command_Struct.MIndex = 0;
                                _Command_Struct.Index = 0;
                            }
                                           
                            if (mIndex == LastOrderMindex)
                            {
                                enableCommands();
                                LastOrderPrio = 228;
                                Initiate_Order();
                            }
                        }

                        i++;

                    }

                    Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + " Finished \r\n"));

                    break;

            }
        }

        public void enableCommands()
        {
            foreach (Command_Struct _Command_Struct in CmdStrct)
            {
                if (_Command_Struct.Enabled == true && _Command_Struct.MaxUpdrH == 0)
                {
                    _Command_Struct.MIndex = 0;
                    _Command_Struct.Index = 0;
                    _Command_Struct.Run = true;
                }
            }
        }

        public class Command_Struct : ObservableObject
        {
            private int _OrdersDone = 0;
            public int OrdersDone
            {
                get { return _OrdersDone; }
                set
                {
                    _OrdersDone = value;
                    OnPropertyChanged();
                }
            }

            private TimeSpan AvgTimeTotal = TimeSpan.Zero;
            public void calcAverage(TimeSpan ts)
            {
                OrdersDone += 1;

                TimeSpan SumTs = AvgTimeTotal.Add(ts);
                AvgTimeTotal = SumTs;

                TimeSpan DivTs = AvgTimeTotal.Divide(OrdersDone);
                TimeSpan RoundedTs = TimeSpan.FromSeconds(Math.Round(DivTs.TotalSeconds, 1));

                AvgTime = RoundedTs;
            }
            private int _index; // Index för att länka lokalt och System manager uppdrag.
            public int Index 
            { 
                get { return _index; }
                set 
                { 
                    _index = value;
                    OnPropertyChanged();
                }
            }            

            private TimeSpan _avgTime = TimeSpan.Zero;
            public TimeSpan AvgTime
            { 
                get { return _avgTime; }
                set 
                { 
                    _avgTime = value;
                    OnPropertyChanged();
                }
            }

            private int _mIndex = 0;
            public int MIndex
            {
                get { return _mIndex; }
                set
                {
                    _mIndex = value;
                    OnPropertyChanged();
                }
            }// Index från System manager

            private bool _TimeRun = true;
            public bool TimeRun
            { 
                get { return _TimeRun; }
                set
                {
                    _TimeRun = value;
                    OnPropertyChanged();
                } 
            }
            public bool Run { get; set; }

            private bool _Enabled;
            public bool Enabled 
            { 
                get { return _Enabled; } 
                set
                {
                    _Enabled = value;
                    OnPropertyChanged();
                }
            }

            private DateTime _OrderStartTime = DateTime.Now.AddHours(-1);
            public DateTime OrderStartTime 
            {
                get { return _OrderStartTime; }
                set
                {
                    _OrderStartTime = value;
                    OnPropertyChanged();
                }
            }
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

            byte[] ordIndex = BitConverter.GetBytes(cmdstrct.MIndex);

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

        private void ManualRun_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewListButton_Click(object sender, RoutedEventArgs e)
        {
            ViewListWindow _ViewListWindow = new ViewListWindow();
            _ViewListWindow.Show();
        }
    }
}
