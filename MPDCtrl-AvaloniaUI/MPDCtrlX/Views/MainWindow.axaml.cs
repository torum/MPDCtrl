using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;

namespace MPDCtrlX.Views;

public partial class MainWindow : AppWindow//Window
{
    public MainWindow()
    {
        // Just for the <Window.InputBindings>
        DataContext = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();

        InitializeComponent();

        var os = Environment.OSVersion;
        Debug.WriteLine("Current OS Information:");
        Debug.WriteLine("Platform: {0:G}", os.Platform);
        Debug.WriteLine("Version String: {0}", os.VersionString);
        Debug.WriteLine("Version Information:");
        Debug.WriteLine("   Major: {0}", os.Version.Major);
        Debug.WriteLine("   Minor: {0}", os.Version.Minor);
        Debug.WriteLine("Service Pack: '{0}'", os.ServicePack);

        if (os.Platform.ToString().StartsWith("Win"))
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            //TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
            //Background = Brushes.Transparent;

            // Only on Windows
            ExtendClientAreaToDecorationsHint = true;
        }
        else
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            //TransparencyLevelHint = [WindowTransparencyLevel.None];
            //TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
            //Background = Brushes.Transparent;
            //Background = this.FindResource("ThemeBackgroundBrush") as IBrush;

            // Not currently supported on Linux due to X11.
            ExtendClientAreaToDecorationsHint = false;
        }
    }
}
