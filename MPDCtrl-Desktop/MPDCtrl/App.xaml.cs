using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MPDCtrl.Contracts;
using MPDCtrl.Helpers;
using MPDCtrl.Models;
using MPDCtrl.Services;
using MPDCtrl.ViewModels;
using MPDCtrl.Views;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace MPDCtrl;

public partial class App : Application
{
    // Mutex to prevent multiple instances.
    private readonly bool _mutexOn = true;

    /// <summary>The event mutex name.</summary>
    private const string _uniqueEventName = "{ff3032a5-315d-40f5-a729-43b8a310f09a}";

    /// <summary>The unique mutex name.</summary>
    private const string _uniqueMutexName = "{fcf95901-d7f3-42da-bcbe-87a3dbd13b8b}";

    /// <summary>The event wait handle.</summary>
    private readonly EventWaitHandle? _eventWaitHandle;

    /// <summary>The mutex.</summary>
    private readonly Mutex? _mutex;

    //public static MainWindow? MainWin { get; private set; }

    private void AppOnStartup(object sender, StartupEventArgs e)
    {

    }

    public IHost AppHost { get; private set; }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.AppHost!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        AppHost = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<IMpcService, MpcService>();
                    services.AddTransient<IBinaryDownloader, BinaryDownloader>();
                })
                .Build();

        if (_mutexOn)
        {
            _mutex = new Mutex(true, _uniqueMutexName, out bool isOwned);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, _uniqueEventName);

            // So, R# would not give a warning that this variable is not used.
            GC.KeepAlive(_mutex);

            if (isOwned)
            {
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (_eventWaitHandle.WaitOne())
                        {
                            Current.Dispatcher.BeginInvoke(
                                (Action)(() => ((MainWindow)Current.MainWindow).BringToForeground()));
                        }
                    })
                {
                    // It is important mark it as background otherwise it will prevent app from exiting.
                    IsBackground = true
                };

                thread.Start();
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            _eventWaitHandle.Set();

            // Terminate this instance.
            this.Shutdown();
        }

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        // For testing.
        //ChangeTheme("LightTheme");
        //ChangeTheme("DarkTheme");

        // For testing only. Don't forget to comment this out if you uncomment.
        //MPDCtrl.Properties.Resources.Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US"); //or ja-JP etc

        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();

        //  For the white background flickering issue. Paints background black in order to prevent "white flashing" when startup/resizing/maximizing window.
        //IntPtr hWnd = new WindowInteropHelper(mainWindow).EnsureHandle();
        //UseImmersiveDarkMode(hWnd, true);

        //  For the white background flickering issue, in addition to paint background, blur it.
        //WindowBlur.SetIsEnabled(mainWindow.WindowChromeBorder, true);

        mainWindow.Show();
        mainWindow.Activate();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();

        base.OnExit(e);
    }

    private readonly StringBuilder _errortxt = new();
    public bool IsSaveErrorLog;
    public string? LogFilePath;

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var exception = e.Exception as Exception;

        System.Diagnostics.Debug.WriteLine("App_DispatcherUnhandledException: " + exception.Message);

        AppendErrorLog("App_DispatcherUnhandledException", exception.Message);

        SaveErrorLog();

        e.Handled = false;
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception.InnerException is not null)
        {
            var exception = e.Exception.InnerException as Exception;

            System.Diagnostics.Debug.WriteLine("TaskScheduler_UnobservedTaskException: " + exception?.Message);

            AppendErrorLog("TaskScheduler_UnobservedTaskException", exception?.Message);

            // save
            SaveErrorLog();
        }

        e.SetObserved();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        if (exception is TaskCanceledException exp)
        {
            // can ignore.
            System.Diagnostics.Debug.WriteLine("CurrentDomain_UnhandledException (TaskCanceledException): " + exp.Message);

            AppendErrorLog("CurrentDomain_UnhandledException (TaskCanceledException)", exp.Message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("CurrentDomain_UnhandledException: " + exception?.Message);

            AppendErrorLog("CurrentDomain_UnhandledException", exception?.Message);

            // save
            SaveErrorLog();
        }

        // TODO: Exit?
        //Environment.Exit(1);
    }

    public void AppendErrorLog(string exceptionKind, string? exceptionMessage)
    {
        DateTime dt = DateTime.Now;
        string nowString = dt.ToString("yyyy/MM/dd HH:mm:ss");

        _errortxt.AppendLine(nowString + " - " + exceptionMessage + " - " + exceptionKind);
    }

    public void SaveErrorLog()
    {
        if (!IsSaveErrorLog)
            return;

        if (string.IsNullOrEmpty(LogFilePath))
            return;

        string s = _errortxt.ToString();
        if (!string.IsNullOrEmpty(s))
            File.WriteAllText(LogFilePath, s);
    }

    public static void ChangeTheme(string themeName)
    {
        ResourceDictionary? _themeDict = Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source == new Uri("pack://application:,,,/Themes/DarkTheme.xaml"));
        if (_themeDict is not null)
        {
            _themeDict.Clear();
        }
        else
        {
            _themeDict = new ResourceDictionary();
            Application.Current.Resources.MergedDictionaries.Add(_themeDict);
        }

        string themeUri = String.Format("pack://application:,,,/Themes/{0}.xaml", themeName);
        _themeDict.Source = new Uri(themeUri);
    }

    /*
    // Paints background black in order to prevent "white flashing" when startup/resizing/maximizing window.
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
    {
        if (IsWindows10OrGreater(17763))
        {
            var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
            if (IsWindows10OrGreater(18985))
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            }

            int useImmersiveDarkMode = enabled ? 1 : 0;
            return DwmSetWindowAttribute(handle, (int)attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
        }

        return false;
    }

    private static bool IsWindows10OrGreater(int build = -1)
    {
        return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
    }
    */
}
