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
using System.Text.RegularExpressions;
using System.Globalization;
using System.Windows.Controls.Primitives;

namespace Command_Transmission
{   
    public partial class MainWindow : Window
    {   
        public static ObservableCollection<Command_Struct> CmdStrct = new ObservableCollection<Command_Struct>();
        private DateTime startTime;
        public static TcpClient tcpClient = new TcpClient();
        public static float TimeScale = 1;

        public static Stopwatch pWatch = new Stopwatch();
        public System.Timers.Timer _Timer;
        public System.Timers.Timer _Timer2;

        private bool poll = false;
        private bool mainProgRun = false;
        private static int LastOrderMindex = 0;
        private static int CurrentOrderMindex = 0;
        private static int LastOrderPrio = 228;
        private static int CurrentOrderIndex = 0;
        
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
            CmdStrct.Add(new Command_Struct() { Enabled = true, Run = true, StartTs = 1, Prio = 0 , TimeRun = true});
            
            DG1.ItemsSource = CmdStrct;
            this.DataContext = this;
           
        }
        private void Add_button_Click(object sender, RoutedEventArgs e)
        {
            if (mainProgRun == true)
            {
                CmdStrct.Add(new Command_Struct() { Run = true, StartTs = 1, Prio = 0, TimeRun = true });
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

            if (tcpClient != null && tcpClient.Client.Connected && mainProgRun == false)
            {
                startTime = DateTime.Now;
                mainProgRun = true;

                setTimer();

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
                if (index != -1)
                {
                    CancelmIndexList.Add(CmdStrct[index].MIndex);
                }
            }

            ModifyMessage.CancelTasks(CancelmIndexList);
        }
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            TextBoxAppend("Program stopped \r\n");
            MessageBoxAppend("Program stopped \r\n");
            mainProgRun = false;
        }

        private void ManualRun_Button_Click(object sender, RoutedEventArgs e)
        {
            ManualAdd _ManualAdd = new ManualAdd();
            _ManualAdd.ShowDialog();
        }

        private void ViewListButton_Click(object sender, RoutedEventArgs e)
        {
            ViewListWindow _ViewListWindow = new ViewListWindow();
            _ViewListWindow.Show();
        }

        private void Show_OrderView(object sender, RoutedEventArgs e)
        {
            Text_Out.Visibility = Visibility.Visible;
            Text_Message.Visibility = Visibility.Collapsed;

        }
        private void Show_MessageView(object sender, RoutedEventArgs e)
        {
            Text_Out.Visibility = Visibility.Collapsed;
            Text_Message.Visibility = Visibility.Visible;
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var CancelIndex in DG1.SelectedItems)
            {
                int index = DG1.Items.IndexOf(CancelIndex);
                if (index != -1)
                {
                    CmdStrct[index].Enabled = true;
                }
            }
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var CancelIndex in DG1.SelectedItems)
            {
                int index = DG1.Items.IndexOf(CancelIndex);
                if (index != -1)
                {
                    CmdStrct[index].Enabled = false;
                }
            }
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var StringScale = ((ComboBoxItem)TimeScaleComboBox.SelectedItem).Content.ToString();
            TimeScale = float.Parse(StringScale, CultureInfo.InvariantCulture.NumberFormat);
        }
        private void setTimer()
        {
            _Timer = new System.Timers.Timer(1000 / TimeScale);
            _Timer.Elapsed += OnTimedEvent;
            _Timer.AutoReset = true;

            _Timer2 = new System.Timers.Timer(10);
            _Timer2.Elapsed += Timer2Elapsed;
            _Timer2.AutoReset = true;
        }
        public void OnTimedEvent( Object State, ElapsedEventArgs e)
        {
            Initiate_Order();
        }

        private void columnHeader_Click(object sender, RoutedEventArgs e)
        {
            var ColumnHeader = sender as DataGridColumnHeader;
            if (ColumnHeader != null)
            {

                string oldName = DG1.Columns[ColumnHeader.DisplayIndex].Header.ToString();

                string newName = NameWindow.GetName(oldName);

                if (newName != null && newName != "")
                {
                    DG1.Columns[ColumnHeader.DisplayIndex].Header = newName;
                }
            }
        }

        public void Timer2Elapsed(Object State, ElapsedEventArgs e)
        {
            if (pWatch.IsRunning)
            {
                TimeSpan Ts = pWatch.Elapsed;
                TimeSpan TimeScaledTs = TimeSpan.FromTicks((long)(Ts.Ticks * TimeScale));

                if (CurrentOrderIndex <= CmdStrct.Count)
                {

                    CmdStrct[CurrentOrderIndex].Time = TimeScaledTs; 

                    if (CmdStrct[CurrentOrderIndex].Time > CmdStrct[CurrentOrderIndex].MaxTs && CmdStrct[CurrentOrderIndex].MaxTid != "00:00.00")
                    {
                        if (CmdStrct[CurrentOrderIndex].OverMaxTs != true)
                        {
                            Dispatcher.Invoke(() => DG1.UnselectAll());
                            Dispatcher.Invoke(() => TextBoxAppend("Order " + CurrentOrderMindex + ": Over Time limit \r\n"));
                        }
                        CmdStrct[CurrentOrderIndex].OverMaxTs = true;
                    }
                }

            }
        }

        public void MainRead()
        {            
            var ns = tcpClient.GetStream();
            var clearBuffer = new byte[4096];
            
            while (ns.DataAvailable)
            {
                ns.Read(clearBuffer, 0 , clearBuffer.Length);
            }

            StartUpReset();
            enableCommands();

            foreach (Command_Struct command_Struct in CmdStrct)
            {
                command_Struct.StartReset();
            }

            _Timer.Start();
            _Timer2.Start();

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

                        string mType = "";

                        Byte[] mValByte = { rMessage[9], rMessage[8] };
                        Byte[] mIndexByte = { rMessage[13], rMessage[12] };
                        Byte[] HeaderLenght = { rMessage[3], rMessage[2] };
                        Byte[] MessageLenght = { rMessage[5], rMessage[4] };
                      
                        int mVal = BitConverter.ToInt16(mValByte);
                        int mIndex = BitConverter.ToInt16(mIndexByte);
                        int hLenght = BitConverter.ToInt16(HeaderLenght);
                        int mLenght = BitConverter.ToInt16(MessageLenght);

                        int TotalLenght = hLenght * 2 + mLenght * 2;

                        StringBuilder hex = new StringBuilder();

                        foreach (byte b in rMessage)
                        {
                            hex.AppendFormat("{0:X2}", b);
                        }

                        if (mVal == 98)
                        {
                            BMessage(rMessage[15], mIndex);
                            Initiate_Order();

                            mType = "B-Message: ";
                        }

                        else if (mVal == 115)
                        {
                            Byte[] bArray = { rMessage[17], rMessage[16] };
                            short bType = BitConverter.ToInt16(bArray, 0);

                            SMessage(bType, mIndex);

                            mType = "S-Message: ";
                        }

                        if (mVal != 69)
                        {
                            Dispatcher.Invoke(() => MessageBoxAppend(mType + hex.ToString(0, TotalLenght) + "\r\n"));
                        }
            


                        Array.Clear(rMessage, 0, rMessage.Length);
 
                    }
                }
            }

            Dispatcher.Invoke(() => ViewListWindow.ViewList.Clear());

            pWatch.Stop();
            pWatch.Reset();

            _Timer.Stop();
            _Timer2.Stop();

            StopCancel();

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

                    TimeSpan TimeScaledTsLastOrder = TimeSpan.FromTicks((long)(tsLastOrder.Ticks * TimeScale));

                    if (TimeSpan.Compare(TimeScaledTsLastOrder, tsPerOrder) == 1 && _Command_Struct.TimeRun == true && CurrentOrderMindex != LastOrderMindex)
                    {
                        _Command_Struct.Run = true;
                        _Command_Struct.TimeRun = false;
                        _Command_Struct.MIndex = 0;
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
                    aMessage = MessageCreate.qMessageCreate(_Command_Struct);

                    var ns = tcpClient.GetStream();
                    ns.Write(aMessage, 0, aMessage.Length);

                    return;

                }

                index++;

            }
            return;
        }

        public void SMessage(int type, int mIndex)
        {

            switch (type)
            {
                case 4096: //Order ready to pick up
                    {

                        int i = 0;

                        foreach (Command_Struct _Command_Struct in CmdStrct)
                        {
                            if (_Command_Struct.MIndex == mIndex)
                            {

                                _Command_Struct.OrderStartTime = DateTime.Now;

                                pWatch.Start();

                                CurrentOrderMindex = mIndex;
                                CurrentOrderIndex = i;

                                //Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + ": Picked up \r\n"));

                            }

                            i++;

                        }

                        Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + ": Ready to pick upp \r\n"));
                        break;
                    }

                case 4097: //Order Loaded
                    {

                        /*
                        int i = 0;

                        foreach (Command_Struct _Command_Struct in CmdStrct)
                        {
                            if (_Command_Struct.MIndex == mIndex)
                            {

                                _Command_Struct.OrderStartTime = DateTime.Now;

                                pWatch.Start();

                                CurrentOrderMindex = mIndex;
                                CurrentOrderIndex = i;

                                Dispatcher.Invoke(() => TextBoxAppend("Order " + mIndex + ": Picked up \r\n"));                          
                                
                            }

                            i++;

                        }
                        */
                        break;
                    }
            
            }
        }

        public void BMessage(int type, int MIndex)
        {
            switch (type)
            {
                case 00: // Order Rejected

                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Rejected \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 01: // Order Acknowledged

                    int AckInd = 1;

                    foreach (Command_Struct _Command_Struct in CmdStrct)
                    {

                        if (_Command_Struct.Index == AckInd && _Command_Struct.MIndex == 0)
                        {
                            _Command_Struct.MIndex = MIndex;
                            _Command_Struct.InQue = true;

                            VisualCommand_Struct _VisualCommand_Struct = ViewListWindow.ViewListStruct(_Command_Struct);

                            Dispatcher.Invoke(() => ViewListWindow.ViewList.Add(_VisualCommand_Struct));

                            Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Acknowledged \r\n"));

                            if (_Command_Struct.Prio <= LastOrderPrio && _Command_Struct.MaxUpdrH == 0) {
                                LastOrderMindex = MIndex;
                                LastOrderPrio = _Command_Struct.Prio;
                            }

                            return;
                        }

                        AckInd++;

                    }

                    break;

                case 03: // Order Finished

                    int ViewIndex = ViewListWindow.ViewList.IndexOf(ViewList.Where(X => X.MIndex == MIndex).FirstOrDefault());
                    if (ViewIndex != -1)
                    {
                        Dispatcher.Invoke(() => ViewListWindow.ViewList.RemoveAt(ViewIndex));
                    }

                    int CmdIndex = CmdStrct.IndexOf(CmdStrct.Where(x => x.MIndex == MIndex).FirstOrDefault());
                    if (CmdIndex != -1)
                    {
                        Command_Struct _Command_Struct = CmdStrct[CmdIndex];

                        if (_Command_Struct.OrderCancelled == false)
                        {
                            pWatch.Stop();
                            TimeSpan Ts = pWatch.Elapsed;

                            TimeSpan TimeScaledTs = TimeSpan.FromTicks((long)(Ts.Ticks * TimeScale));
                            pWatch.Reset();

                            _Command_Struct.calcAverage(TimeScaledTs);
                            _Command_Struct.Time = TimeScaledTs;

                            if (_Command_Struct.Time > _Command_Struct.MaxTime)
                            {
                                _Command_Struct.MaxTime = TimeScaledTs;
                            }
                        }

                        _Command_Struct.TimeRun = true;
                        _Command_Struct.InQue = false;
                        _Command_Struct.OrderCancelled = false;

                        if (_Command_Struct.MaxUpdrH != 0)
                        {
                            _Command_Struct.Index = 0;
                        }

                        if (_Command_Struct.IsManual == true)
                        {
                            Dispatcher.Invoke(() => CmdStrct.RemoveAt(CmdIndex));
                        }

                        LastMessageCheck();

                        if (MIndex == LastOrderMindex)
                        {
                            enableCommands();
                            LastOrderPrio = 228;
                            Initiate_Order();
                        }
                    }

                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Finished \r\n"));
                    break;

                case 04: // Order Cancelled
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Cancelled \r\n"));
                    break;

                case 08: //Invalid numbers of parameters
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Invalid numbers of parameters \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 09: //Priority error
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Priority error \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 10: //Invalid Structure
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Invalid Structure \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 11: //Order Buffer full
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Order Buffer full \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 15: //Param number too high
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Param number too high \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 16: //Not allowed to update
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Not allowed to update \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 26: //MIssing Params
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": MIssing Params \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 27: //Duplicate ikey
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Duplicate ikey \r\n"));
                    ErrorMessageFix(MIndex);
                    break;

                case 28: //Invalid Format code
                    Dispatcher.Invoke(() => TextBoxAppend("Order " + MIndex + ": Invalid Format code \r\n"));
                    ErrorMessageFix(MIndex);
                    break;
            }

            return;
        }

        public void ErrorMessageFix(int MIndex)
        {
            foreach (Command_Struct _Command_struct in CmdStrct) 
            { 
                if (MIndex == _Command_struct.MIndex)
                {
                    var ns = tcpClient.GetStream();
                    _Command_struct.Enabled = false;

                    byte[] aMessage = new byte[34];
                    aMessage = MessageCreate.qMessageCreate(_Command_struct);

                    ns.Write(aMessage, 0, aMessage.Length);
                }
            }
        }

        public static class ModifyMessage
        {

            public static void ManualSend(Command_Struct _CmdStruct)
            {

                _CmdStruct.IsManual = true;
                _CmdStruct.Enabled = true;
                _CmdStruct.Run = true;
                
                CmdStrct.Add(_CmdStruct);

                LastMessageCheck();
                return;
            }

            public static void CancelTasks(List<int> CancelmIndex)
            {

                var ns = tcpClient.GetStream();

                try
                {
                    foreach (var _CancelmIndex in CancelmIndex)
                    {

                        foreach (Command_Struct _Command_Struct in CmdStrct)
                        {

                            if (_Command_Struct.MIndex == _CancelmIndex)
                            {

                                _Command_Struct.OrderCancelled = true;
                                _Command_Struct.Enabled = false;
                                _Command_Struct.Run = false;

                                if (LastOrderMindex == _CancelmIndex)
                                {
                                    LastMessageCheck();
                                }

                                if (CurrentOrderMindex != _Command_Struct.MIndex)
                                {
                                    var nMessage = MessageCreate.nMessageCreate(_Command_Struct);
                                    ns.Write(nMessage, 0, nMessage.Length);
                                }

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
        }

        public static void enableCommands()
        {            
            foreach (Command_Struct _Command_Struct in CmdStrct)
            {
                if (_Command_Struct.Enabled == true && _Command_Struct.MaxUpdrH == 0 && _Command_Struct.Run == false && _Command_Struct.IsManual == false)
                {
                    _Command_Struct.MIndex = 0;
                    _Command_Struct.Index = 0;
                    _Command_Struct.Run = true;
                    _Command_Struct.InQue = false;
                    _Command_Struct.OverMaxTs = false;
                }
            }
        }
        public static void LastMessageCheck()
        {

            int TempMIndex = 0;
            int TempPrio = 228;

            foreach (Command_Struct _Command_Struct in CmdStrct)
            {
                if (_Command_Struct.Enabled == true && _Command_Struct.MaxUpdrH == 0 && _Command_Struct.InQue == true)
                {
                    if (_Command_Struct.Prio <= TempPrio)
                    {
                        TempMIndex = _Command_Struct.MIndex;
                        TempPrio = _Command_Struct.Prio;
                    }
                }
            }

            if (TempMIndex != 0)
            {
                LastOrderMindex = TempMIndex;
                LastOrderPrio = TempPrio;
            }

            return;
        
        }

        public void StopCancel()
        {

            var CancelmIndexList = new List<int>(); ;

            foreach (Command_Struct _Command_Struct in CmdStrct)
            {
                if (_Command_Struct.InQue == true)
                {
                    _Command_Struct.InQue = false;
                    CancelmIndexList.Add(_Command_Struct.MIndex);
                }
            }

            ViewList.Clear();

            ModifyMessage.CancelTasks(CancelmIndexList);
        }

        public void StartUpReset()
        {
            CurrentOrderIndex = 0;
            CurrentOrderMindex = 0;
            LastOrderMindex = 0;
            LastOrderPrio = 228;
        }
        public void TextBoxAppend(string text)
        {
            string s = DateTime.Now.ToString("HH:mm:ss");

            Text_Out.AppendText(s + ". " + text);
            Text_Out.CaretIndex = Text_Out.Text.Length;
            Text_Out.ScrollToEnd();
            return;
        }

        public void MessageBoxAppend(string text)
        {
            string s = DateTime.Now.ToString("HH:mm:ss");

            Text_Message.AppendText(s + ". " + text);
            Text_Message.CaretIndex = Text_Message.Text.Length;
            Text_Message.ScrollToEnd();
            return;
        }

        public class ObservableObject : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class Command_Struct : ObservableObject
        {
            //MainWindow _MainWindow = new MainWindow();

            public void StartReset()
            {
                OrdersDone = 0;
                AvgTimeTotal = TimeSpan.Zero;
                AvgTime = TimeSpan.Zero;
                Time = TimeSpan.Zero;
                MaxTime = TimeSpan.Zero;
                OrderCancelled = false;
                Index = 0;
                OverMaxTs = false;
                TimeRun = true;
                Run = true;
                InQue = false;
                MIndex = 0;
            }

            public void EnableCommand()
            {
                Run = true;
                MIndex = 0;
                Index = 0;
                TimeRun = true;
                LastMessageCheck();
            }

            public bool OrderCancelled = false;

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
            private int _index = 0; // Index för att länka lokalt och System manager uppdrag.
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
            }

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

            private TimeSpan _Time;

            public TimeSpan Time
            {
                get { return _Time; }
                set
                {
                    _Time = value;
                    OnPropertyChanged();
                }
            }

            private TimeSpan _MaxTime = TimeSpan.Zero;
            public TimeSpan MaxTime
            {
                get { return _MaxTime; }
                set
                {
                    _MaxTime = value;
                    OnPropertyChanged();
                }
            }

            private bool _Run = true;
            public bool Run
            {
                get { return _Run; }
                set
                {
                    _Run = value;
                    OnPropertyChanged();
                }
            }

            private bool _Enabled = false;
            public bool Enabled
            {
                get { return _Enabled; }
                set
                {
                    _Enabled = value;
                    if (value == true && Run == false && InQue == false)
                    {
                        EnableCommand();
                    }
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

            private bool _IsManual = false;
            public bool IsManual
            {
                get { return _IsManual; }
                set
                {
                    _IsManual = value;
                    OnPropertyChanged();
                }
            }

            private string _MaxTs = "00:00.00";
            public string MaxTid
            {
                get { return _MaxTs; }
                set
                {
                    _MaxTs = value;
                    TextToSt();
                    OnPropertyChanged();
                }
            }
            public TimeSpan MaxTs { get; set; }
            public void TextToSt()
            {
                try
                {
                    TimeSpan ts = TimeSpan.ParseExact(MaxTid, @"mm\:ss\.ff", CultureInfo.InvariantCulture);
                    MaxTs = ts;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            private bool _OverMaxTs = false;
            public bool OverMaxTs
            {
                get { return _OverMaxTs; }
                set
                {
                    if (_OverMaxTs != value || value == true)
                    {

                    }
                    _OverMaxTs = value;
                    OnPropertyChanged();
                }
            }
            private bool _InQue = false;
            public bool InQue
            {
                get { return _InQue; }
                set
                {
                    _InQue = value;
                    OnPropertyChanged();
                }
            }

            public int MaxUpdrH { get; set; }
            public int Prio { get; set; }
            public int StartTs { get; set; }
            public int KördH { get; set; }
            public int Param0 { get; set; }
            public int Param1 { get; set; }
            public int Param2 { get; set; }
            public int Param3 { get; set; }
            public int Param4 { get; set; }
            public int Param5 { get; set; }
            public int Param6 { get; set; }
            public int Param7 { get; set; }
            public int Param8 { get; set; }
            public int Param9 { get; set; }
        }
        public class MessageCreate
        {
            public static byte[] nMessageCreate(Command_Struct cmdstrct)
            {
                Byte b1 = 0x87; Byte b2 = 0xCD; // Header
                Byte b3 = 0x00; Byte b4 = 0x08; // Size of header 
                Byte b5 = 0x00; Byte b6 = 0x06; // Size of Message
                Byte b7 = 0x00; Byte b8 = 0x01; // Function code
                Byte b9 = 0x00; Byte b10 = 0x6E; // Message Type
                Byte b11 = 0x00; Byte b12 = 0x02; // Number of params

                byte[] ordIndex = BitConverter.GetBytes(cmdstrct.MIndex);

                Byte b13 = ordIndex[1];
                Byte b14 = ordIndex[0];

                Byte[] nMessage = { b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14 };
                return nMessage;
            }
            public static byte[] qMessageCreate(Command_Struct cmdStrct)
            {
                Byte b1 = 0x87; Byte b2 = 0xCD; // Header
                Byte b3 = 0x00; Byte b4 = 0x08; // Size of header 
                Byte b5 = 0x00; Byte b6 = 0x16; // Size of message
                Byte b7 = 0x00; Byte b8 = 0x01; // function code
                Byte b9 = 0x00; Byte b10 = 0x71; // q-meddelande
                Byte b11 = 0x00; Byte b12 = 0x0C; // Antal Parametrar

                Byte b13 = (byte)cmdStrct.StartTs;
                Byte b14 = (byte)cmdStrct.Prio;

                byte[] Param0 = BitConverter.GetBytes(cmdStrct.Param0);

                Byte b15 = Param0[1];
                Byte b16 = Param0[0];

                byte[] Param1 = BitConverter.GetBytes(cmdStrct.Param1);

                Byte b17 = Param1[1];
                Byte b18 = Param1[0];

                Byte b19 = (byte)(cmdStrct.Param2); Byte b20 = (byte)(cmdStrct.Param2 >> 8);
                Byte b21 = (byte)(cmdStrct.Param3); Byte b22 = (byte)(cmdStrct.Param3 >> 8);
                Byte b23 = (byte)(cmdStrct.Param4); Byte b24 = (byte)(cmdStrct.Param4 >> 8);
                Byte b25 = (byte)(cmdStrct.Param5); Byte b26 = (byte)(cmdStrct.Param5 >> 8);
                Byte b27 = (byte)(cmdStrct.Param6); Byte b28 = (byte)(cmdStrct.Param6 >> 8);
                Byte b29 = (byte)(cmdStrct.Param7); Byte b30 = (byte)(cmdStrct.Param7 >> 8);
                Byte b31 = (byte)(cmdStrct.Param8); Byte b32 = (byte)(cmdStrct.Param8 >> 8);
                Byte b33 = (byte)(cmdStrct.Param9); Byte b34 = (byte)(cmdStrct.Param9 >> 8);


                Byte[] aMessage = { b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, b11, b12, b13, b14, b15, b16, b17, b18, b19, b20, b21, b22, b23, b24, b25, b26, b27, b28, b29, b30, b31, b32, b33, b34 };

                return aMessage;
            }
        }

        
    }
}
