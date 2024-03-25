using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using static Command_Transmission.MainWindow;

namespace Command_Transmission
{
    public partial class ViewListWindow : Window
    {

        public static ObservableCollection<VisualCommand_Struct> ViewList = new ObservableCollection<VisualCommand_Struct>();
        public ViewListWindow()
        {
            InitializeComponent();
            ViewListDG.ItemsSource = ViewList;
            this.DataContext = ViewList;
        }

        private void StackPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                var CancelmIndexList = new List<int>();

                foreach (VisualCommand_Struct visualCommand_Struct in ViewListDG.SelectedItems)
                {
                    CancelmIndexList.Add(visualCommand_Struct.MIndex);
                }

                ModifyMessage.CancelTasks(CancelmIndexList);
            }
        }
        public static VisualCommand_Struct ViewListStruct(Command_Struct _Cmd_Struct)
        {
            VisualCommand_Struct TempCmd_Struct = new VisualCommand_Struct();

            TempCmd_Struct.MIndex = _Cmd_Struct.MIndex;

            TempCmd_Struct.UppAddr = _Cmd_Struct.UppAddr;
            TempCmd_Struct.AvAddr = _Cmd_Struct.AvAddr;

            TempCmd_Struct.Prio = _Cmd_Struct.Prio;

            TempCmd_Struct.Param3 = _Cmd_Struct.Param3;
            TempCmd_Struct.Param4 = _Cmd_Struct.Param4;
            TempCmd_Struct.Param5 = _Cmd_Struct.Param5;
            TempCmd_Struct.Param6 = _Cmd_Struct.Param6;
            TempCmd_Struct.Param7 = _Cmd_Struct.Param7;
            TempCmd_Struct.Param8 = _Cmd_Struct.Param8;
            TempCmd_Struct.Param9 = _Cmd_Struct.Param9;
            TempCmd_Struct.Param10 = _Cmd_Struct.Param10;

            TempCmd_Struct.MaxUpdhrH = _Cmd_Struct.MaxUpdrH;

            return TempCmd_Struct;
        }
        public class VisualCommand_Struct : MainWindow.ObservableObject
        {

            private int _MIndex;
            public int MIndex
            {
                get { return _MIndex; }
                set
                {
                    _MIndex = value;
                    OnPropertyChanged();
                }
            }

            private int _UppAddr;
            public int UppAddr
            {
                get { return _UppAddr; }
                set
                {
                    _UppAddr = value;
                    OnPropertyChanged();
                }
            }

            private int _AvAddr;

            public int AvAddr
            {
                get { return _AvAddr; }
                set
                {
                    _AvAddr = value;
                    OnPropertyChanged();
                }
            }


            private int _Prio;
            public int Prio
            {
                get { return _Prio; }
                set
                {
                    _Prio = value;
                    OnPropertyChanged();
                }
            }

            private int _Param3;
            public int Param3
            {
                get { return _Param3; }
                set
                {
                    _Param3 = value;
                    OnPropertyChanged();
                }
            }

            private int _Param4;
            public int Param4
            {
                get { return _Param4; }
                set
                {
                    _Param4 = value;
                    OnPropertyChanged();
                }
            }

            public int _Param5;
            public int Param5
            {
                get { return _Param5; }
                set
                {
                    _Param5 = value;
                    OnPropertyChanged();
                }
            }

            private int _Param6;
            public int Param6
            {
                get { return _Param6; }
                set
                {
                    _Param6 = value;
                    OnPropertyChanged();
                }
            }

            private int _Param7;
            public int Param7
            {
                get { return _Param7; }
                set
                {
                    _Param7 = value;
                    OnPropertyChanged();
                }
            }

            private int _Param8;
            public int Param8
            {
                get { return _Param8; }
                set
                {
                    _Param8 = value;
                    OnPropertyChanged();
                }
            }

            private int _Param9;
            public int Param9
            {
                get { return _Param9; }
                set
                {
                    _Param9 = value;
                    OnPropertyChanged();
                }
            }

            private int _Param10;
            public int Param10
            {
                get { return _Param10; }
                set
                {
                    _Param10 = value;
                    OnPropertyChanged();
                }
            }

            private int _MaxUpdhrH;
            public int MaxUpdhrH
            {
                get { return _MaxUpdhrH; }
                set
                {
                    _MaxUpdhrH = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
