using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using MPDCtrl.Helpers;
using MPDCtrl.Services;
using MPDCtrl.Services.Contracts;
using MPDCtrl.ViewModels;
using MPDCtrl.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using WinRT.Interop;

namespace MPDCtrl;

public partial class App : Application
{
    // AppDataFolder
    private static readonly string _appName = "MPDCtrl4";//_resourceLoader.GetString("AppName");
    private static readonly string _appDeveloper = "torum";
    private static readonly string _envDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static readonly string AppName = _appName;
    public static string AppDataFolder { get; private set; } = System.IO.Path.Combine(System.IO.Path.Combine(_envDataFolder, _appDeveloper), _appName);//_envDataFolder + System.IO.Path.DirectorySeparatorChar + _appDeveloper + System.IO.Path.DirectorySeparatorChar + _appName;
    public static string AppConfigFilePath { get; private set; } = System.IO.Path.Combine(AppDataFolder, _appName + ".config");

    // Temp album cover cache folder.
    private static readonly string _envAppLocalFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // Use Local instead of temp path. //System.IO.Path.GetTempPath();
    private static readonly string _envAppLocalAppFolder = System.IO.Path.Combine((System.IO.Path.Combine(_envAppLocalFolder, _appDeveloper)), AppName);
    public static string AppDataCacheFolder { get; private set; } = System.IO.Path.Combine(_envAppLocalAppFolder, "AlbumCoverCache");

    // ErrorLog
    private static readonly string _logFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + "MPDCtrl4_errors.txt";

    // MainWindow
    public static MainWindow? MainWnd
    {
        get; private set;
    }

    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        if (RuntimeHelper.IsMSIX)
        {
            Debug.WriteLine("IsMSIX");
            var envDataFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            AppDataFolder = System.IO.Path.Combine(System.IO.Path.Combine(envDataFolder, _appDeveloper), _appName);
            AppConfigFilePath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.AppName + ".config");
            var envAppLocalCahceFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
            AppDataCacheFolder = System.IO.Path.Combine(System.IO.Path.Combine((System.IO.Path.Combine(envAppLocalCahceFolder, _appDeveloper)), AppName), "AlbumCoverCache");
        }
        else
        {
            /*
            Debug.WriteLine("Not IsMSIX");
            AppDataFolder = System.IO.Path.Combine(System.IO.Path.Combine(_envDataFolder, _appDeveloper), _appName);
            AppConfigFilePath = System.IO.Path.Combine(AppDataFolder, _appName + ".config");
            AppDataCacheFolder = System.IO.Path.Combine(_envAppLocalAppFolder, "AlbumCoverCache");
            */
        }

        // Only works in packaged environment.
        //Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
        //System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en-US", false);
        //Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "ja-JP";
        //System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo( "ja-JP", false );

        // Force theme
        //this.RequestedTheme = ApplicationTheme.Dark;
        //this.RequestedTheme = ApplicationTheme.Light;

        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Core Services
            services.AddSingleton<IMpcService, MpcService>();
            services.AddSingleton<IMpcBinaryService, MpcBinaryService>();
            services.AddSingleton<IDialogService, DialogService>();

            // Views and ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ShellPage>();

            // Pages
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<QueuePage>();
            services.AddSingleton<AlbumsPage>();
            services.AddSingleton<AlbumDetailPage>();
            services.AddSingleton<ArtistsPage>();
            services.AddSingleton<FilesPage>();
            services.AddSingleton<SearchPage>();
            services.AddTransient<PlaylistItemPage>();

            // Configuration
            //services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        Microsoft.UI.Xaml.Application.Current.UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Single instance.
        // https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/applifecycle
        var mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("MPDCtrlMain");
        // If the instance that's executing the OnLaunched handler right now
        // isn't the "main" instance.
        if (!mainInstance.IsCurrent)
        {
            // Redirect the activation (and args) to the "main" instance, and exit.
            var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            await mainInstance.RedirectActivationToAsync(activatedEventArgs);

            System.Diagnostics.Process.GetCurrentProcess().Kill();
            return;
        }
        else
        {
            // Otherwise, register for activation redirection
            Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += App_Activated;
        }

        //MainWnd = new(); // < No
        MainWnd = App.GetService<MainWindow>();

        // Too late here. In order to set themes, sets content in MainWindow constructor.
        //MainWnd.Content = App.GetService<ShellPage>();

        //MainWnd.Activate(); // Activate won't work..
        MainWnd.AppWindow.Show();
    }

    private void App_Activated(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments e)
    {
        if (MainWnd is null)
        {
            return;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            if (MainWnd is not null)
            {
                MainWnd.Activate();

                //MainWindow?.BringToFront();
                IntPtr hWnd = WindowNative.GetWindowHandle(MainWnd);
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE); // Ensure it's not minimized
                NativeMethods.SetForegroundWindow(hWnd); // Attempt to set it as the foreground window
            }
        });
    }

    #region == BringToFront ==

    private static partial class NativeMethods
    {

        internal const int SW_RESTORE = 9; // Restores a minimized window and brings it to the foreground.

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    }

    #endregion

    #region == UnhandledException ==

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.

        // This does not fire...because of winui3 bugs. should be fixed in v1.2.2 WinAppSDK
        // see https://github.com/microsoft/microsoft-ui-xaml/issues/5221

        // This kills app... 
        // https://github.com/microsoft/microsoft-ui-xaml/issues/10447

        Debug.WriteLine("App_UnhandledException", e.Message + $"StackTrace: {e.Exception.StackTrace}, Source: {e.Exception.Source}");
        AppendErrorLog("App_UnhandledException", e.Message + $"StackTrace: {e.Exception.StackTrace}, Source: {e.Exception.Source}");

        try
        {
            SaveErrorLog();
        }
        catch (Exception) { }

        e.Handled = true;
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception.InnerException is not Exception exception)
        {
            return;
        }

        Debug.WriteLine("TaskScheduler_UnobservedTaskException: " + exception.Message);
        AppendErrorLog("TaskScheduler_UnobservedTaskException", exception.Message);
        SaveErrorLog();

        e.SetObserved();
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
        {
            return;
        }

        if (exception is TaskCanceledException)
        {
            // can ignore.
            Debug.WriteLine("CurrentDomain_UnhandledException (TaskCanceledException): " + exception.Message);
            AppendErrorLog("CurrentDomain_UnhandledException (TaskCanceledException)", exception.Message);
        }
        else
        {
            Debug.WriteLine("CurrentDomain_UnhandledException: " + exception.Message);
            AppendErrorLog("CurrentDomain_UnhandledException", exception.Message);
            SaveErrorLog();
        }
    }

    private static readonly StringBuilder Errortxt = new();

    public static void AppendErrorLog(string kindTxt, string errorTxt)
    {
        Errortxt.AppendLine(kindTxt + ": " + errorTxt);
        var dt = DateTime.Now;
        Errortxt.AppendLine($"Occured at {dt.ToString("yyyy/MM/dd HH:mm:ss")}");
        Errortxt.AppendLine("");
    }

    public static void SaveErrorLog()
    {
        if (string.IsNullOrEmpty(_logFilePath))
        {
            return;
        }

        if (Errortxt.Length > 0)
        {
            Errortxt.AppendLine("");
            var dt = DateTime.Now;
            Errortxt.AppendLine($"Saved at {dt.ToString("yyyy/MM/dd HH:mm:ss")}");

            var s = Errortxt.ToString();
            if (!string.IsNullOrEmpty(s))
            {
                File.WriteAllText(_logFilePath, s);
            }
        }
    }

    #endregion
}




