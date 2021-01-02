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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using MPDCtrl.ViewModels;
using MPDCtrl.Views;

namespace MPDCtrl.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (this.DataContext as MainViewModel).OnWindowLoaded;
            ContentRendered += (this.DataContext as MainViewModel).OnContentRendered;
            Closing += (this.DataContext as MainViewModel).OnWindowClosing;

            RestoreButton.Visibility = Visibility.Collapsed;
            MaxButton.Visibility = Visibility.Visible;


            if (this.DataContext is MainViewModel vm)
            {
                if (vm != null)
                {
                    vm.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };

                    vm.DebugWindowShowHide += () => OnDebugWindowShowHide();

                    vm.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };
                    vm.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };

                    vm.DebugCommandClear += () => OnDebugCommandClear();
                    vm.DebugIdleClear += () => OnDebugIdleClear();

                    vm.AckWindowOutput += (sender, arg) => { this.OnAckWindowOutput(arg); };

                    vm.AckWindowClear += () => OnAckWindowClear();

                }
            }
        }

        public void OnScrollIntoView(int arg)
        {
            if (QueueListview.Items.Count > arg)
            {
                QueueListview.ScrollIntoView(QueueListview.Items[arg]);

                ListViewItem lvi = QueueListview.ItemContainerGenerator.ContainerFromIndex(arg) as ListViewItem;
                if (lvi != null)
                    lvi.Focus();
            }
        }

        public void OnDebugCommandOutput(string arg)
        {
            // AppendText() is much faster than data binding.
            DebugCommandTextBox.AppendText(arg);

            DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
            DebugCommandTextBox.ScrollToEnd();
        }

        public void OnDebugIdleOutput(string arg)
        {
            // AppendText() is much faster than data binding.
            DebugIdleTextBox.AppendText(arg);

            DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
            DebugIdleTextBox.ScrollToEnd();
        }

        public void OnDebugCommandClear()
        {
            DebugCommandTextBox.Clear();
        }

        public void OnDebugIdleClear()
        {
            DebugIdleTextBox.Clear();
        }

        public void OnDebugWindowShowHide()
        {
            if (DebugWindowGridSplitter.Visibility == Visibility.Visible)
            {
                LayoutGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);

                LayoutGrid.RowDefinitions[3].Height = new GridLength(0);
                LayoutGrid.RowDefinitions[4].Height = new GridLength(0);

                DebugWindowGridSplitter.Visibility = Visibility.Collapsed;
                DebugWindow.Visibility = Visibility.Collapsed;
            }
            else
            {
                LayoutGrid.RowDefinitions[2].Height = new GridLength(3, GridUnitType.Star);

                LayoutGrid.RowDefinitions[3].Height = new GridLength(8);
                LayoutGrid.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);

                DebugWindowGridSplitter.Visibility = Visibility.Visible;
                DebugWindow.Visibility = Visibility.Visible;
            }
        }

        public void OnAckWindowOutput(string arg)
        {
            // AppendText() is much faster than data binding.
            AckTextBox.AppendText(arg);

            AckTextBox.CaretIndex = AckTextBox.Text.Length;
            AckTextBox.ScrollToEnd();
        }

        public void OnAckWindowClear()
        {
            AckTextBox.Clear();
        }

        public void BringToForeground()
        {
            if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            }

            this.Activate();
            this.Focus();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                RestoreButton.Visibility = Visibility.Collapsed;
                MaxButton.Visibility = Visibility.Visible;
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                RestoreButton.Visibility = Visibility.Visible;
                MaxButton.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void QueueListview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Not really good especially when multiple items are selected
            //QueueListview.ScrollIntoView(QueueListview.SelectedItem);
        }

        private void TextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender != null)
            {
                if (sender is TextBox)
                {
                    if ((sender as TextBox).Visibility == Visibility.Visible)
                    {
                        (sender as TextBox).Focus();
                        Keyboard.Focus((sender as TextBox));
                    }
                }
            }
        }

        private void DialogInputTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            /*
            if (DialogInputTextBox.Visibility == Visibility.Visible)
            {
                DialogInputTextBox.Focus();
                Keyboard.Focus(DialogInputTextBox);
            }
            */
        }

        private void PasswordBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender != null)
            {
                if (sender is PasswordBox)
                {
                    var pb = (sender as PasswordBox);
                    if (pb.Visibility == Visibility.Visible)
                    {
                        if (pb.Focusable)
                        {
                            Keyboard.Focus(pb);
                            pb.Focus();
                        }
                    }
                }
            }

            /*
            if (DialogInputTextBox.Visibility == Visibility.Visible)
            {
                DialogInputTextBox.Focus();
                Keyboard.Focus(DialogInputTextBox);
            }
            */
        }

        private void DialogButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender != null)
            {
                if (sender is Button)
                {
                    if ((sender as Button).Visibility == Visibility.Visible)
                    {
                        (sender as Button).Focus();
                        Keyboard.Focus((sender as Button));
                    }
                }
            }
        }

        private void PlaylistNameWithNewComboBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender != null)
            {
                if (sender is ComboBox)
                {
                    if ((sender as ComboBox).Visibility == Visibility.Visible)
                    {
                        (sender as ComboBox).Focus();
                        Keyboard.Focus((sender as ComboBox));
                    }
                }
            }
        }


        // リンクをクリックして、ブラウザ起動して表示
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(e.Uri.AbsoluteUri);
            psi.UseShellExecute = true;
            Process.Start(psi);
            e.Handled = true;
        }





        #region == MAXIMIZE時のタスクバー被りのFix ==
        // https://engy.us/blog/2020/01/01/implementing-a-custom-window-title-bar-in-wpf/

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
        }

        public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
                // including the task bar.
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                // Adjust the maximized size and position to fit the work area of the correct monitor
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                    GetMonitorInfo(monitor, ref monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top) - 4; // -4を付加した。てっぺんをクリックしても反応がなかったから。
                    mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top) + 4; // 付加した分の補正。
                }

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }

        private const int WM_GETMINMAXINFO = 0x0024;

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }


        #endregion

    }
}
