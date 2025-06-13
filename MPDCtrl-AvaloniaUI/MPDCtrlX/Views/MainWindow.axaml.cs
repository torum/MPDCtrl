using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;

namespace MPDCtrlX.Views;

public partial class MainWindow : AppWindow//Window
{
    public MainWindow()
    {
        InitializeComponent();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        var os = Environment.OSVersion;
        Debug.WriteLine("Current OS Information:\n");
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
            TransparencyLevelHint = [WindowTransparencyLevel.None];

            ExtendClientAreaToDecorationsHint = false;

            //Background = this.FindResource("ThemeBackgroundBrush") as IBrush;
        }
    }
}
