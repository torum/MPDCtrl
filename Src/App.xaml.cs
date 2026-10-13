using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using MPDCtrl.Helpers;
using MPDCtrl.Services;
using MPDCtrl.Services.Contracts;
using MPDCtrl.ViewModels;
using MPDCtrl.Views;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinRT.Interop;

namespace MPDCtrl;

public sealed partial class App : Application
{
    // AppDataFolder
    public const string AppName = "MPDCtrl4";
    private const string AppDeveloper = "torum";
    private static readonly string _envDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string AppDataFolder { get; private set; } = System.IO.Path.Combine(System.IO.Path.Combine(_envDataFolder, AppDeveloper), AppName);
    public static string AppConfigFilePath { get; private set; } = System.IO.Path.Combine(AppDataFolder, AppName + ".config");

    // Temp album cover cache folder.
    private static readonly string _envAppLocalFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // Use Local instead of temp path. //System.IO.Path.GetTempPath();
    private static readonly string _envAppLocalAppFolder = System.IO.Path.Combine((System.IO.Path.Combine(_envAppLocalFolder, AppDeveloper)), AppName);
    public static string AlbumCoverCacheFolder { get; private set; } = System.IO.Path.Combine(_envAppLocalAppFolder, "AlbumCoverCache");

    // ErrorLog
    private static readonly string _logFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + "MPDCtrl4_errors.txt";
    private readonly StringBuilder _errortxt = new();
    private readonly Lock _errorLogLock = new();
    //
    private Task? _appHostStartTask;
    private Task? _appHostStopTask;

#if DEBUG
    public bool IsSaveErrorLog { get; set; } = true;
#else
    public bool IsSaveErrorLog { get; set; } = false;
#endif

    private Microsoft.UI.Dispatching.DispatcherQueue? CurrentDispatcherQueue { get; }

    public IHost Host
    {
        get;
    }

    /*
    public static T GetService<T>()
        where T : class
    {
        if ((Application.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }
    */
    public static T GetService<T>() where T : class
    {
        return ((App)Current).Host.Services.GetRequiredService<T>();
    }

    public App()
    {
        CurrentDispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        if (RuntimeHelper.IsMSIX)
        {
            Debug.WriteLine("IsMSIX");
            var envDataFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            AppDataFolder = System.IO.Path.Combine(System.IO.Path.Combine(envDataFolder, AppDeveloper), AppName);
            AppConfigFilePath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, AppName + ".config");
            //AppConfigFilePath = System.IO.Path.Combine(AppDataFolder, AppName + ".config"); // Ops. should have been this...
            var envAppLocalCahceFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
            AlbumCoverCacheFolder = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(envAppLocalCahceFolder, AppDeveloper), AppName), "AlbumCoverCache");
        }
        else
        {
            //Debug.WriteLine("Not IsMSIX");
        }

        try
        {
            if (!System.IO.Directory.Exists(App.AlbumCoverCacheFolder))
            {
                System.IO.Directory.CreateDirectory(App.AlbumCoverCacheFolder);
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            AppendErrorLog("Failed to create AlbumCoverCache folder on startup.", ex.ToString());
            SaveErrorLog();
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
            ConfigureLogging((context, logging) =>
            {
#if DEBUG
                logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Debug);
                logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
#else
                // Strips log destinations and shuts down the framework engines for production
                logging.ClearProviders(); 
                logging.AddFilter(null, LogLevel.None); 
#endif
            }).
            ConfigureServices((context, services) =>
            {
                // Core Services
                services.AddSingleton<IMpcService, MpcService>();
                services.AddSingleton<IMpcBinaryService, MpcBinaryService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IDispatcherService>(new DispatcherService(CurrentDispatcherQueue));

                // Views and ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<ShellPage>();

                // Pages (Frame creates page instances)
                // Currently WinUI's Frame.Navigate(Type) does not provide a way for applications to control how a Page instance is created)
                // So, no DI.
                //services.AddSingleton<SettingsPage>();
                //services.AddSingleton<QueuePage>();
                //services.AddSingleton<AlbumsPage>();
                //services.AddSingleton<AlbumDetailPage>();
                //services.AddSingleton<ArtistsPage>();
                //services.AddSingleton<FilesPage>();
                //services.AddSingleton<SearchPage>();
                //services.AddTransient<PlaylistItemPage>();

                // Configuration
                //services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            }).
            Build();

        Microsoft.UI.Xaml.Application.Current.UnhandledException += App_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        //this.UnhandledException += App_UnhandledException;// already subscribed above
    }

    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
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

                //System.Diagnostics.Process.GetCurrentProcess().Kill();
                Environment.Exit(0);
                return;
            }
            else
            {
                // Otherwise, register for activation redirection
                Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += App_Activated;
            }

            _appHostStartTask = StartAppHostAsync();
            var main = App.GetService<MainWindow>();
            main.Closed += (sender, e) =>
            {
                //_appHostStopTask = StopAppHostAsync();
                ShutdownAppHost();
            };
            //main.AppWindow.Show(true);
            main.Activate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnLaunched: {ex}");
            AppendErrorLog("OnLaunched", ex.ToString());
            SaveErrorLog();

            // Startup failed and the app is in an unknown state, so exit instead of leaving a half-initialized process.
            Environment.Exit(1);
        }
    }

    private async Task StartAppHostAsync()
    {
        try
        {
            await Host.StartAsync();
        }
        catch (Exception ex)
        {
            AppendErrorLog("Host.StartAsync", ex.ToString());
            SaveErrorLog();
        }
    }

    private static readonly TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(5);

    private void ShutdownAppHost()
    {
        try
        {
            // Run on the thread pool so awaits inside don't capture the UI SynchronizationContext (avoids deadlock).
            _appHostStopTask ??= Task.Run(StopAppHostAsync);

            if (!_appHostStopTask.Wait(_shutdownTimeout))
            {
                AppendErrorLog("ShutdownAppHost", $"Host did not stop within {_shutdownTimeout.TotalSeconds} seconds.");
                SaveErrorLog();
            }
        }
        catch (Exception ex)
        {
            AppendErrorLog("ShutdownAppHost", ex.ToString());
            SaveErrorLog();
        }
    }

    private async Task StopAppHostAsync()
    {
        try
        {
            if (_appHostStartTask != null)
            {
                await _appHostStartTask.ConfigureAwait(false);
            }

            await Host.StopAsync(_shutdownTimeout).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppendErrorLog("Host.StopAsync", ex.ToString());
            SaveErrorLog();
        }
        finally
        {
            try
            {
                Host.Dispose();
                Debug.WriteLine($"[{DateTimeOffset.UtcNow:O}] Host is disposed.");
            }
            catch (Exception ex)
            {
                AppendErrorLog("Host.Dispose", ex.ToString());
                SaveErrorLog();
            }
        }
    }

    private void App_Activated(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments e)
    {
        CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            var main = App.GetService<Views.MainWindow>();

            main.Activate();

            //MainWindow?.BringToFront();
            IntPtr hWnd = WindowNative.GetWindowHandle(main);
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE); // Ensure it's not minimized
            NativeMethods.SetForegroundWindow(hWnd); // Attempt to set it as the foreground window
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
        // "A notable limitation is that the UnhandledException event arguments don’t contain as much detail as the original exception as propagated from app code."

        // This does not fire...because of winui3 bugs.
        // see https://github.com/microsoft/microsoft-ui-xaml/issues/5221

        // This kills app... 
        // https://github.com/microsoft/microsoft-ui-xaml/issues/10447


        // e.Exception can lose detail when it crosses the WinRT boundary,
        // but ToString() still gives the best available type, message, HResult and stack trace.
        var details = $"{e.Exception}{Environment.NewLine}HResult: 0x{e.Exception.HResult:X8}{Environment.NewLine}Message: {e.Message}";

        Debug.WriteLine($"App_UnhandledException: {details}");
        AppendErrorLog("App_UnhandledException", details);
        SaveErrorLog();

        // Mark as handled only for exceptions that are known to be recoverable.
        // Leaving it unhandled lets the app terminate, which is safer for unknown states.
        if (e.Exception is OperationCanceledException)
        {
            e.Handled = true;
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Flatten so nested AggregateExceptions are unwrapped, and log full details (type, message, stack trace).
        var details = e.Exception.Flatten().ToString();

        Debug.WriteLine($"TaskScheduler_UnobservedTaskException: {details}");
        AppendErrorLog("TaskScheduler_UnobservedTaskException", details);
        SaveErrorLog();

        e.SetObserved();
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
        {
            var raw = e.ExceptionObject?.ToString() ?? "Unknown exception object";
            Debug.WriteLine($"CurrentDomain_UnhandledException: {raw}");
            AppendErrorLog("CurrentDomain_UnhandledException", raw);
            SaveErrorLog();
            return;
        }

        if (exception is TaskCanceledException)
        {
            // can ignore.
            Debug.WriteLine($"CurrentDomain_UnhandledException (TaskCanceledException): {exception.Message}");
            return;
        }

        // Log full details (type, message, stack trace, inner exceptions).
        var details = exception.ToString();

        Debug.WriteLine($"CurrentDomain_UnhandledException (IsTerminating={e.IsTerminating}): {details}");
        AppendErrorLog("CurrentDomain_UnhandledException", details);
        SaveErrorLog();
    }

    public void AppendErrorLog(string kindTxt, string errorTxt)
    {
        if (!IsSaveErrorLog)
        {
            return;
        }
        var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
        lock (_errorLogLock)
        {
            _errortxt.AppendLine(kindTxt + ": " + errorTxt);
            _errortxt.AppendLine(CultureInfo.InvariantCulture, $"Occurred at {timestamp}");
        }
    }

    public void SaveErrorLog()
    {
        if (!IsSaveErrorLog)
        {
            return;
        }

        if (string.IsNullOrEmpty(_logFilePath))
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

        lock (_errorLogLock)
        {
            if (_errortxt.Length == 0)
            {
                return;
            }

            _errortxt.AppendLine(CultureInfo.InvariantCulture, $"Saved at {timestamp}");
            _errortxt.AppendLine();

            try
            {
                File.WriteAllText(_logFilePath, _errortxt.ToString());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The error logger must never throw from inside an exception handler.
                Debug.WriteLine($"SaveErrorLog failed: {ex.Message}");
            }
        }
    }

    #endregion
}
