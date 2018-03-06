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
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Globalization;

namespace WpfMPD
{
    // Code behind - View.
    // All the things related to VIEW and Windows specific matter.

    // from AcrylicWPF example
    //https://github.com/bbougot/AcrylicWPF

    internal enum AccentState
    {
        ACCENT_DISABLED = 1,
        ACCENT_ENABLE_GRADIENT = 0,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_INVALID_STATE = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // For the acrylic effects. Win32API? Cross-platform? What is it? w
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        public MainWindow()
        {
            //MPDCtrl.Properties.Resources.Culture = CultureInfo.GetCultureInfo("en-US"); //or ja-JP

            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // For the acrylic effects
            EnableBlur();

            if ((MPDCtrl.Properties.Settings.Default.MainWindow_Left != 0) && MPDCtrl.Properties.Settings.Default.MainWindow_Top != 0) {
                Left = MPDCtrl.Properties.Settings.Default.MainWindow_Left;
                Top = MPDCtrl.Properties.Settings.Default.MainWindow_Top;
            }
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
            if (WindowState == WindowState.Normal)
            {
                MPDCtrl.Properties.Settings.Default.MainWindow_Left = Left;
                MPDCtrl.Properties.Settings.Default.MainWindow_Top = Top;
            }
            MPDCtrl.Properties.Settings.Default.TopMost = this.Topmost;
            MPDCtrl.Properties.Settings.Default.Save();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            //UWP test complains about this. So, disabled.
            //System.Diagnostics.Process.Start("https://github.com/torumyax/MPD-Ctrl");
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


    /*
    //When listview slection changes, automatically show selected item in the view area.
    public static class ItemsControlExtensions
    {
        public static void ScrollToCenterOfView(this ItemsControl itemsControl, object item)
        {
            // Scroll immediately if possible
            if (!itemsControl.TryScrollToCenterOfView(item))
            {
                // Otherwise wait until everything is loaded, then scroll
                if (itemsControl is ListBox) ((ListBox)itemsControl).ScrollIntoView(item);
                itemsControl.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    itemsControl.TryScrollToCenterOfView(item);
                }));
            }
        }
        
        private static bool TryScrollToCenterOfView(this ItemsControl itemsControl, object item)
        {
            // Find the container
            var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
            if (container == null) return false;

            // Find the ScrollContentPresenter
            ScrollContentPresenter presenter = null;
            for (Visual vis = container; vis != null && vis != itemsControl; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if ((presenter = vis as ScrollContentPresenter) != null)
                    break;
            if (presenter == null) return false;

            // Find the IScrollInfo
            var scrollInfo =
                !presenter.CanContentScroll ? presenter :
                presenter.Content as IScrollInfo ??
                FirstVisualChild(presenter.Content as ItemsPresenter) as IScrollInfo ??
                presenter;

            // Compute the center point of the container relative to the scrollInfo
            Size size = container.RenderSize;
            Point center = container.TransformToAncestor((Visual)scrollInfo).Transform(new Point(size.Width / 2, size.Height / 2));
            center.Y += scrollInfo.VerticalOffset;
            center.X += scrollInfo.HorizontalOffset;

            // Adjust for logical scrolling
            if (scrollInfo is StackPanel || scrollInfo is VirtualizingStackPanel)
            {
                double logicalCenter = itemsControl.ItemContainerGenerator.IndexFromContainer(container) + 0.5;
                Orientation orientation = scrollInfo is StackPanel ? ((StackPanel)scrollInfo).Orientation : ((VirtualizingStackPanel)scrollInfo).Orientation;
                if (orientation == Orientation.Horizontal)
                    center.X = logicalCenter;
                else
                    center.Y = logicalCenter;
            }

            // Scroll the center of the container to the center of the viewport
            if (scrollInfo.CanVerticallyScroll) scrollInfo.SetVerticalOffset(CenteringOffset(center.Y, scrollInfo.ViewportHeight, scrollInfo.ExtentHeight));
            if (scrollInfo.CanHorizontallyScroll) scrollInfo.SetHorizontalOffset(CenteringOffset(center.X, scrollInfo.ViewportWidth, scrollInfo.ExtentWidth));
            return true;
        }

        private static double CenteringOffset(double center, double viewport, double extent)
        {
            return Math.Min(extent - viewport, Math.Max(0, center - viewport / 2));
        }
        private static DependencyObject FirstVisualChild(Visual visual)
        {
            if (visual == null) return null;
            if (VisualTreeHelper.GetChildrenCount(visual) == 0) return null;
            return VisualTreeHelper.GetChild(visual, 0);
        }
        */

    /*
    // ScrollIntoViewForListView Behavior
    //https://stackoverflow.com/questions/10135850/how-do-i-scrollintoview-after-changing-the-filter-on-a-listview-in-a-mvvm-wpf
    //https://stackoverflow.com/questions/33597483/wpf-mvvm-scrollintoview
    //https://stackoverflow.com/questions/16866309/listbox-scroll-into-view-with-mvvm
    
    public class ScrollIntoViewForListView : Behavior<ListView>
    {
        /// <summary>
        ///  When Beahvior is attached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
        }

        /// <summary>
        /// On Selection Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AssociatedObject_SelectionChanged(object sender,
                                               SelectionChangedEventArgs e)
        {
            if (sender is ListView)
            {
                ListView listview = (sender as ListView);
                if (listview.SelectedItem != null)
                {
                    listview.Dispatcher.BeginInvoke(
                        (Action)(() =>
                        {
                            listview.UpdateLayout();
                            if (listview.SelectedItem !=
                            null)
                                listview.ScrollIntoView(
                                listview.SelectedItem);
                        }));
                }
            }
        }
        /// <summary>
        /// When behavior is detached
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.SelectionChanged -=
                AssociatedObject_SelectionChanged;

        }
    }
    */

    /*
        // don't need to use this..anymore.
        public class PathButton : Button
        {
            public static DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(Geometry), typeof(PathButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(Data_Changed)));

            public Geometry Data
            {
                get { return (Geometry)GetValue(DataProperty); }
                set { SetValue(DataProperty, value); }
            }

            private static void Data_Changed(DependencyObject o, DependencyPropertyChangedEventArgs args)
            {
                PathButton thisClass = (PathButton)o;
                thisClass.SetData();
            }

            private void SetData()
            {
                Path path = new Path();
                path.Data = Data;
                path.Stretch = Stretch.Uniform;
                path.Fill = this.Foreground;//Brushes.Gainsboro;
                path.Stroke = this.Foreground; //Brushes.Gainsboro;//this.Foreground;
                path.StrokeThickness = 0.5;
                this.Content = path;
            }
        }

        public class PathToggleButton : ToggleButton
        {
            public static DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(Geometry), typeof(PathToggleButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(Data_Changed)));

            public Geometry Data
            {
                get { return (Geometry)GetValue(DataProperty); }
                set { SetValue(DataProperty, value); }
            }

            private static void Data_Changed(DependencyObject o, DependencyPropertyChangedEventArgs args)
            {
                PathToggleButton thisClass = (PathToggleButton)o;
                thisClass.SetData();
            }

            private void SetData()
            {
                Path path = new Path();
                path.Data = Data;
                path.Stretch = Stretch.Uniform;
                path.Fill = this.Foreground;//Brushes.Gainsboro;
                path.Stroke = this.Foreground; //Brushes.Gainsboro;//this.Foreground;
                path.StrokeThickness = 0.5;
                this.Content = path;
            }
        }
        */

}
