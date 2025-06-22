using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.Models;
using MPDCtrlX.ViewModels;
using MPDCtrlX.Views;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MPDCtrlX.Views;

public partial class MainWindow : Window//AppWindow//
{
    private readonly MainView? shell = (App.Current as App)?.AppHost.Services.GetRequiredService<MainView>();
    private readonly SettingsPage? settings = (App.Current as App)?.AppHost.Services.GetRequiredService<SettingsPage>();

    public MainWindow()
    {
        // Just for the <Window.InputBindings>
        this.DataContext = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();

        InitializeComponent();

        this.navigateView.Content = shell;

        if (this.DataContext is MainViewModel vm)
        {
            vm.CurrentSongChanged += (sender, arg) => OnCurrentSongChanged(arg);

            vm.DebugWindowShowHide += () => OnDebugWindowShowHide();
            //vm.DebugWindowShowHide2 += (sender, arg) => OnDebugWindowShowHide2(arg);
            vm.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };
            vm.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };
            //vm.DebugCommandClear += () => OnDebugCommandClear();
            //vm.DebugIdleClear += () => OnDebugIdleClear();
            vm.AckWindowOutput += (sender, arg) => { this.OnAckWindowOutput(arg); };
            vm.AckWindowClear += () => OnAckWindowClear();

        }

        var os = Environment.OSVersion;
        /*
        Debug.WriteLine("Current OS Information:");
        Debug.WriteLine("Platform: {0:G}", os.Platform);
        Debug.WriteLine("Version String: {0}", os.VersionString);
        Debug.WriteLine("Version Information:");
        Debug.WriteLine("   Major: {0}", os.Version.Major);
        Debug.WriteLine("   Minor: {0}", os.Version.Minor);
        Debug.WriteLine("Service Pack: '{0}'", os.ServicePack);
        */

        if (os.Platform.ToString().StartsWith("Win"))
        {
            //TitleBar.ExtendsContentIntoTitleBar = true;
            //TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            
            //TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            //TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
            //Background = Brushes.Transparent;

            // Only on Windows
            //ExtendClientAreaToDecorationsHint = true;
        }
        else
        {
            //TitleBar.ExtendsContentIntoTitleBar = true;
            //TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            
            //TransparencyLevelHint = [WindowTransparencyLevel.None];
            //TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
            //Background = Brushes.Transparent;
            //Background = this.FindResource("ThemeBackgroundBrush") as IBrush;

            // Not currently supported on Linux due to X11.
            //ExtendClientAreaToDecorationsHint = false;
        }

        // TODO: not working for current Avalonia UI.
        this.Activated += (sender, e) => { shell?.WindowActivated();  };
        this.Deactivated += (sender, e) => { shell?.WindowDeactivated(); };
    }

    private void OnCurrentSongChanged(string msg)
    {
        this.Title = msg;
    }

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

    private void NavigationView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not NavigationView)
        {
            return;
        }

        if (this.DataContext is MainViewModel vm)
        {
            var hoge = this.navigateView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
            if (hoge != null)
            {
                hoge.IsSelected = true;
            }

            vm.SelectedNodeMenu = vm.MainMenuItems.FirstOrDefault();
            if (vm.SelectedNodeMenu != null)
            {
                vm.SelectedNodeMenu.Selected = true;
            }
        }
    }

    private void NavigationView_SelectionChanged(object? sender, FluentAvalonia.UI.Controls.NavigationViewSelectionChangedEventArgs e)
    {
        if (sender is not NavigationView)
        {
            return;
        }

        if (this.DataContext is MainViewModel vm)
        {
            if (e.SelectedItem is NodeMenuPlaylists pl)
            {
                // don't change page here.
                pl.Selected = false;
                if (vm.SelectedNodeMenu != null)
                {
                    vm.SelectedNodeMenu.Selected = true;
                }
            }
            else
            {
                vm.SelectedNodeMenu = e.SelectedItem as NodeTree;
            }
        }
    }

    private void NavigationView_ItemInvoked(object? sender, FluentAvalonia.UI.Controls.NavigationViewItemInvokedEventArgs e)
    {
        if (sender is NavigationView nv)
        {
            if (e.IsSettingsInvoked == true)
            {
                nv.Content = settings;
                return;
            }

            if (nv.Content != shell)
            {
                nv.Content = shell;
            }
        }
    }
}
