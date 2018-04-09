using System.Windows;
using System.Windows.Controls;

namespace WpfMPD
{
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        public MainWindow()
        {
            // For testing only. Don't forget to comment this out if you uncomment.
            //MPDCtrl.Properties.Resources.Culture = CultureInfo.GetCultureInfo("en-US"); //or ja-JP

            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //MPDCtrl.Properties.Settings.Default.Upgrade();

            // Load window pos setting.
            if ((MPDCtrl.Properties.Settings.Default.MainWindow_Left != 0) 
                && (MPDCtrl.Properties.Settings.Default.MainWindow_Top != 0)
                && (MPDCtrl.Properties.Settings.Default.MainWindow_Width != 0)
                && (MPDCtrl.Properties.Settings.Default.MainWindow_Height != 0)
                )
            {
                Left = MPDCtrl.Properties.Settings.Default.MainWindow_Left;
                Top = MPDCtrl.Properties.Settings.Default.MainWindow_Top;
                Width = MPDCtrl.Properties.Settings.Default.MainWindow_Width;
                Height = MPDCtrl.Properties.Settings.Default.MainWindow_Height;
            }
            // TopMost opts.
            this.Topmost = MPDCtrl.Properties.Settings.Default.TopMost;
            if (this.Topmost)
            {
                mStayOnTop.IsChecked = true;
            }
            else
            {
                mStayOnTop.IsChecked = false;
            }
        }

        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            SongListView.ScrollIntoView(SongListView.SelectedItem);

            // Listview flickering...
            //SongListView.ScrollToCenterOfView(SongListView.SelectedItem);

        }

        private void MenuItem_mStayOnTop_Click(object sender, RoutedEventArgs e)
        {
            if (this.Topmost) {
                this.Topmost = false;
                mStayOnTop.IsChecked = false;
            }
            else
            {
                this.Topmost = true;
                mStayOnTop.IsChecked = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save window pos.
            if (WindowState == WindowState.Normal)
            {
                MPDCtrl.Properties.Settings.Default.MainWindow_Left = this.Left;
                MPDCtrl.Properties.Settings.Default.MainWindow_Top = this.Top;
                MPDCtrl.Properties.Settings.Default.MainWindow_Height = this.Height;
                MPDCtrl.Properties.Settings.Default.MainWindow_Width = this.Width;
            }
            MPDCtrl.Properties.Settings.Default.TopMost = this.Topmost;
            MPDCtrl.Properties.Settings.Default.Save();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            //APPX packaging test complains about this. So, let's just disable this.
            //System.Diagnostics.Process.Start("https://github.com/torum/MPDCtrl");
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            (sender as PasswordBox).Tag = "";
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PasswordBox.Password = "";
        }
    }

}
