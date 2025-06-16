using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.ViewModels;
using System;
using System.Text;
using System.Diagnostics;

namespace MPDCtrlX.Views;

public partial class MainView : UserControl
{
    private MainViewModel _viewModel;

    public MainView(MainViewModel? vm)
    {
        _viewModel = vm ?? throw new ArgumentNullException(nameof(vm), "MainViewModel cannot be null.");

        InitializeComponent();

        //DataContext = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();//new MainViewModel();
        DataContext = _viewModel;

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

    private void ListBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        test1x.Width = _viewModel.QueueColumnHeaderPositionWidth;
        test2x.Width = _viewModel.QueueColumnHeaderNowPlayingWidth;
        test3x.Width = _viewModel.QueueColumnHeaderTitleWidth;
        test4x.Width = _viewModel.QueueColumnHeaderTimeWidth;
        test5x.Width = _viewModel.QueueColumnHeaderArtistWidth;
        test6x.Width = _viewModel.QueueColumnHeaderAlbumWidth;
        test7x.Width = _viewModel.QueueColumnHeaderDiscWidth;
        test8x.Width = _viewModel.QueueColumnHeaderTrackWidth;
        test9x.Width = _viewModel.QueueColumnHeaderGenreWidth;
        test10x.Width = _viewModel.QueueColumnHeaderLastModifiedWidth;

    }
}
