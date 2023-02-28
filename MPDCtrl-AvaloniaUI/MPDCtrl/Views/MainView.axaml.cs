using Avalonia.Controls;
using MPDCtrl.ViewModels;
using System.Runtime.InteropServices;
using System.Text;

namespace MPDCtrl.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        public void OnLoaded()
        {
            System.Diagnostics.Debug.WriteLine("Init");
            // Event subscriptions
            if (this.DataContext is MainViewModel vm)
            {
                if (vm != null)
                {
                    // Subscribe to Window events.

                    Loaded += vm.OnWindowLoaded;

                    //ContentRendered += vm.OnContentRendered;

                    //Closing += vm.OnWindowClosing;

                    // Subscribe to ViewModel events.

                    vm.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };

                    vm.ScrollIntoViewAndSelect += (sender, arg) => { this.OnScrollIntoViewAndSelect(arg); };

                    vm.DebugWindowShowHide += () => OnDebugWindowShowHide();

                    vm.DebugWindowShowHide2 += (sender, arg) => OnDebugWindowShowHide2(arg);

                    vm.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };

                    vm.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };

                    vm.DebugCommandClear += () => OnDebugCommandClear();

                    vm.DebugIdleClear += () => OnDebugIdleClear();

                    vm.AckWindowOutput += (sender, arg) => { this.OnAckWindowOutput(arg); };

                    vm.AckWindowClear += () => OnAckWindowClear();

                    System.Diagnostics.Debug.WriteLine("DataContext is set");

                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DataContext is null");
            }
        }


        public void OnScrollIntoView(int arg)
        {
            /*
            if ((QueueListview.Items.Count > arg) && (arg > -1))
            {
                QueueListview.ScrollIntoView(QueueListview.Items[arg]);

                ListViewItem lvi = QueueListview.ItemContainerGenerator.ContainerFromIndex(arg) as ListViewItem;
                if (lvi != null)
                    lvi.Focus();
            }
            */
        }

        public void OnScrollIntoViewAndSelect(int arg)
        {
            /*
            if ((QueueListview.Items.Count > arg) && (arg > -1))
            {
                QueueListview.SelectedItems.Clear();

                QueueListview.ScrollIntoView(QueueListview.Items[arg]);

                ListViewItem lvi = QueueListview.ItemContainerGenerator.ContainerFromIndex(arg) as ListViewItem;
                if (lvi != null)
                {
                    //QueueListview.ScrollIntoView(lvi);
                    lvi.Focus();
                    lvi.IsSelected = true;
                }
            }
            */
        }

        private StringBuilder _sbCommandOutput = new StringBuilder();
        public void OnDebugCommandOutput(string arg)
        {
            System.Diagnostics.Debug.WriteLine("arg");

            _sbCommandOutput.Append(arg);
            DebugCommandTextBox.Text = _sbCommandOutput.ToString();
            // AppendText() is much faster than data binding.
            //DebugCommandTextBox.AppendText(arg);

            //DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
            //DebugCommandTextBox.ScrollToEnd();
        }

        private StringBuilder _sbIdleOutput = new StringBuilder();
        public void OnDebugIdleOutput(string arg)
        {
            System.Diagnostics.Debug.WriteLine("arg");

            _sbIdleOutput.Append(arg);
            DebugIdleTextBox.Text = _sbIdleOutput.ToString();
            /*
            // AppendText() is much faster than data binding.
            DebugIdleTextBox.AppendText(arg);

            DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
            DebugIdleTextBox.ScrollToEnd();
            */
        }

        public void OnDebugCommandClear()
        {
            //DebugCommandTextBox.Clear();
        }

        public void OnDebugIdleClear()
        {
            //DebugIdleTextBox.Clear();
        }

        public void OnDebugWindowShowHide()
        {
            /*
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
            */
        }

        public void OnDebugWindowShowHide2(bool on)
        {
            /*
            if (on)
            {
                LayoutGrid.RowDefinitions[2].Height = new GridLength(3, GridUnitType.Star);

                LayoutGrid.RowDefinitions[3].Height = new GridLength(8);
                LayoutGrid.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);

                DebugWindowGridSplitter.Visibility = Visibility.Visible;
                DebugWindow.Visibility = Visibility.Visible;
            }
            else
            {
                LayoutGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);

                LayoutGrid.RowDefinitions[3].Height = new GridLength(0);
                LayoutGrid.RowDefinitions[4].Height = new GridLength(0);

                DebugWindowGridSplitter.Visibility = Visibility.Collapsed;
                DebugWindow.Visibility = Visibility.Collapsed;
            }
            */
        }

        public void OnAckWindowOutput(string arg)
        {
            /*
            // AppendText() is much faster than data binding.
            AckTextBox.AppendText(arg);

            AckTextBox.CaretIndex = AckTextBox.Text.Length;
            AckTextBox.ScrollToEnd();
            */
        }

        public void OnAckWindowClear()
        {
            //AckTextBox.Clear();
        }

    }

}