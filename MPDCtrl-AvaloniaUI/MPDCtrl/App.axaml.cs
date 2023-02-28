using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MPDCtrl.ViewModels;
using MPDCtrl.Views;
using System;
using System.IO;
using System.Text;

namespace MPDCtrl
{
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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };

                // Needs this.
                //desktop.MainWindow.Content = new MainView();
                if (desktop.MainWindow.Content is MainView mv)
                {
                    //mv.Init();
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
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
}