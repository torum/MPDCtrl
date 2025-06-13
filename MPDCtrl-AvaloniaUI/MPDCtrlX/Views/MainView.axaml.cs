using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.ViewModels;
using System;
using System.Text;
using System.Diagnostics;

namespace MPDCtrlX.Views;

public partial class MainView : UserControl
{
    public MainView(MainViewModel? vm)
    {
        InitializeComponent();

        //DataContext = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();//new MainViewModel();
        DataContext = vm ?? throw new ArgumentNullException(nameof(vm), "MainViewModel cannot be null.");

        if (DataContext is MainViewModel)
        {
            // Event subscriptions
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

        var os = Environment.OSVersion;
        if (os.Platform.ToString().StartsWith("Win"))
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(32, GridUnitType.Pixel);
        }
        else
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);

        }
    }

    public MainView()
    {
        InitializeComponent();
    }

    private StringBuilder _sbCommandOutput = new();
    public void OnDebugCommandOutput(string arg)
    {
        // AppendText() is much faster than data binding.
        //DebugCommandTextBox.AppendText(arg);

        _sbCommandOutput.Append(arg);
        DebugCommandTextBox.Text = _sbCommandOutput.ToString();
        DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
    }

    private StringBuilder _sbIdleOutput = new();
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
        DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
    }
}
