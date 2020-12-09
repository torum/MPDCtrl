using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;

namespace MPDCtrl.Helpers
{
    /// <summary>
    /// listview CollapseableColumn
    /// https://www.technical-recipes.com/2017/setting-the-visibility-of-individual-gridviewcolumn-items-in-wpf/
    /// </summary>
    public class GridViewBehaviours
    {
        public static readonly DependencyProperty CollapseableColumnProperty =
           DependencyProperty.RegisterAttached("CollapseableColumn", typeof(bool), typeof(GridViewBehaviours),
              new UIPropertyMetadata(false, OnCollapseableColumnChanged));

        public static bool GetCollapseableColumn(DependencyObject d)
        {
            return (bool)d.GetValue(CollapseableColumnProperty);
        }

        public static void SetCollapseableColumn(DependencyObject d, bool value)
        {
            d.SetValue(CollapseableColumnProperty, value);
        }

        private static void OnCollapseableColumnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var header = sender as GridViewColumnHeader;
            if (header == null)
                return;

            header.IsVisibleChanged += AdjustWidth;
        }

        private static void AdjustWidth(object sender, DependencyPropertyChangedEventArgs e)
        {
            var header = sender as GridViewColumnHeader;
            if (header == null)
                return;

            double restore = double.NaN;

            if (header.Tag != null)
            {
                try
                {
                    restore = double.Parse(header.Tag.ToString());
                }
                catch { }
            }

            header.Column.Width = header.Visibility == Visibility.Collapsed ? 0 : restore;
        }
    }
}
