using Avalonia;
using Avalonia.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrlX.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            AppendErrorLog(ex.Message + System.IO.Path.DirectorySeparatorChar + ex.StackTrace, "Fatal exception in Main");
        }
        finally
        {
            SaveErrorLog();
        }

        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace(LogEventLevel.Warning);


    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception.InnerException is not null)
        {
            AppendErrorLog("TaskScheduler_UnobservedTaskException", e.Exception.InnerException.Message);

            SaveErrorLog();
        }

        e.SetObserved();
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        if (exception is TaskCanceledException exp)
        {
            // can ignore.
            AppendErrorLog("CurrentDomain_UnhandledException (TaskCanceledException)", exp.Message);
        }
        else
        {
            if (exception is not null)
            { 
                AppendErrorLog("CurrentDomain_UnhandledException", exception.Message); 
            }
        }

        SaveErrorLog();

        // TODO: Exit?
        //Environment.Exit(1);
    }

    // Log file.
    private static readonly StringBuilder _errortxt = new();
    private static readonly string _logFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + "MPDCtrlX_exception.txt";

    private static void AppendErrorLog(string errorTxt, string kindTxt)
    {
        DateTime dt = DateTime.Now;
        string nowString = dt.ToString("yyyy/MM/dd HH:mm:ss");

        _errortxt.AppendLine(nowString + " - " + kindTxt + " - " + errorTxt);
    }

    private static void SaveErrorLog()
    {
        if (string.IsNullOrEmpty(_logFilePath))
            return;
#if DEBUG

        string s = _errortxt.ToString();
        if (!string.IsNullOrEmpty(s))
            File.WriteAllText(_logFilePath, s);
#else
#endif

    }
}
