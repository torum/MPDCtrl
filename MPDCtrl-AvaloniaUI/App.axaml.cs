using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MPDCtrlX.ViewModels;
using MPDCtrlX.Views;
using System;
using System.IO;
using System.Text;

namespace MPDCtrlX;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static StringBuilder Errortxt = new StringBuilder();
    public static bool IsSaveErrorLog;
    public static string LogFilePath = string.Empty;
    public static void AppendErrorLog(string errorTxt, string kindTxt)
    {
        DateTime dt = DateTime.Now;
        string nowString = dt.ToString("yyyy/MM/dd HH:mm:ss");

        Errortxt.AppendLine(nowString + " - " + kindTxt + " - " + errorTxt);
    }

    public static void SaveErrorLog()
    {
        if (!IsSaveErrorLog)
            return;

        if (string.IsNullOrEmpty(LogFilePath))
            return;

        string s = Errortxt.ToString();
        if (!string.IsNullOrEmpty(s))
            File.WriteAllText(LogFilePath, s);
    }
}