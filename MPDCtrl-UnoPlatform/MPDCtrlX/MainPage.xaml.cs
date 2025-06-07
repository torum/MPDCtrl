using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using MPDCtrlX.ViewModels;

namespace MPDCtrlX;

public sealed partial class MainPage : Page
{
    public ViewModels.MainViewModel? ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = (Application.Current as App)?.Host?.Services.GetRequiredService<MainViewModel>();


        this.InitializeComponent();


        if (ViewModel is MainViewModel vm)
        {
            if (vm is not null)
            {

                //vm.OnWindowLoaded(this);

                // Subscribe to Window events.

                //Loaded += vm.OnWindowLoaded;
                //Closing += vm.OnWindowClosing;
                //ContentRendered += vm.OnContentRendered;

                // Subscribe to ViewModel events.

                //vm.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };
                //vm.ScrollIntoViewAndSelect += (sender, arg) => { this.OnScrollIntoViewAndSelect(arg); };
                //vm.DebugWindowShowHide += () => OnDebugWindowShowHide();
                //vm.DebugWindowShowHide2 += (sender, arg) => OnDebugWindowShowHide2(arg);
                vm.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };
                vm.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };
                //vm.DebugCommandClear += () => OnDebugCommandClear();
                //vm.DebugIdleClear += () => OnDebugIdleClear();
                //vm.AckWindowOutput += (sender, arg) => { this.OnAckWindowOutput(arg); };
                //vm.AckWindowClear += () => OnAckWindowClear();
            }
        }

    }

    private StringBuilder _sbCommandOutput = new StringBuilder();
    public void OnDebugCommandOutput(string arg)
    {
        // AppendText() is much faster than data binding.
        //DebugCommandTextBox.AppendText(arg);

        _sbCommandOutput.Append(arg);
        DebugCommandTextBox.Text = _sbCommandOutput.ToString();
        //DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
    }

    private StringBuilder _sbIdleOutput = new StringBuilder();
    public void OnDebugIdleOutput(string arg)
    {
        /*
        // AppendText() is much faster than data binding.
        DebugIdleTextBox.AppendText(arg);

        DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
        DebugIdleTextBox.ScrollToEnd();
        */

        //_sbIdleOutput.Append(DebugIdleTextBox.Text);
        _sbIdleOutput.Append(arg);
        DebugIdleTextBox.Text = _sbIdleOutput.ToString();
        //DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
    }
}
