using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.ViewModels;
using System;
using System.Text;
using System.Diagnostics;

namespace MPDCtrlX.Views;

public partial class MainView : UserControl
{
    private readonly MainViewModel? _viewModel;

    public MainView()//MainViewModel? vm
    {
        _viewModel = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();//new MainViewModel();//vm ?? throw new ArgumentNullException(nameof(vm), "MainViewModel cannot be null.");

        InitializeComponent();

        //DataContext = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();//new MainViewModel();
        DataContext = _viewModel;

        if (_viewModel != null)
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
            _viewModel.DebugWindowShowHide += () => OnDebugWindowShowHide();
            //vm.DebugWindowShowHide2 += (sender, arg) => OnDebugWindowShowHide2(arg);
            _viewModel.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };
            _viewModel.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };
            //vm.DebugCommandClear += () => OnDebugCommandClear();
            //vm.DebugIdleClear += () => OnDebugIdleClear();
            _viewModel.AckWindowOutput += (sender, arg) => { this.OnAckWindowOutput(arg); };
            _viewModel.AckWindowClear += () => OnAckWindowClear();
        }

        var os = Environment.OSVersion;
        if (os.Platform.ToString().StartsWith("Win"))
        {
            this.MainGrid.RowDefinitions[0].Height = new GridLength(32, GridUnitType.Pixel);
            this.ImageLogo.IsVisible = true;
        }
        else
        {
            this.MainGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
            this.ImageLogo.IsVisible = false;
        }
    }
    /*
#pragma warning disable CS8618
    public MainView()
#pragma warning restore CS8618
    {
        InitializeComponent();

    }
    */
    private readonly StringBuilder _sbCommandOutput = new();
    public void OnDebugCommandOutput(string arg)
    {
        // AppendText() is much faster than data binding.
        //DebugCommandTextBox.AppendText(arg);

        _sbCommandOutput.Append(arg);
        DebugCommandTextBox.Text = _sbCommandOutput.ToString();
        DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
    }

    private readonly StringBuilder _sbIdleOutput = new();
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

    public void OnDebugWindowShowHide()
    {
        if (this.DebugWindow.IsVisible)
        {
            this.DebugWindow.IsVisible = false;
        }
        else
        {
            this.DebugWindow.IsVisible = true;
        }
    }

}
