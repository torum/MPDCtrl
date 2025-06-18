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

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

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
            TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];

            ExtendClientAreaToDecorationsHint = true;

            //Background = Brushes.Transparent;
        }
        else
        {
            TitleBar.ExtendsContentIntoTitleBar = false;

            TransparencyLevelHint = [WindowTransparencyLevel.None];

            ExtendClientAreaToDecorationsHint = false;

            //Background = this.FindResource("ThemeBackgroundBrush") as IBrush;
        }
    }
}
