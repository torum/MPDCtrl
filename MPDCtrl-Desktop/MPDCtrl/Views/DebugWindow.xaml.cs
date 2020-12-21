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
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Views
{
    public partial class DebugWindow : Window
    {
        private bool _reallyClose = false;

        public DebugWindow()
        {
            InitializeComponent();

            Loaded += DebugWindow_Loaded;
        }

        private void DebugWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load window possition.
        }

        private void DebugTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DebugTextBox.CaretIndex = DebugTextBox.Text.Length;
            DebugTextBox.ScrollToEnd();
        }

        public void SetClose()
        {
            _reallyClose = true;
        }

        private void DebugWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_reallyClose)
            { 
                e.Cancel = true;
                Hide();
            }
        }
    }
}
