using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MPDCtrlX.Contracts;
using MPDCtrlX.Models;
using MPDCtrlX.Services;
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
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                //DataContext = new MainViewModel()

                //Content = new MainView((App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>())
                
                //Content = new MainView()

                //{
                //DataContext = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>()//new MainViewModel()
                //}
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // TODO:
            /*
            singleViewPlatform.MainView = new MainView
            {
                //DataContext = new MainViewModel()
            };
            */
            //singleViewPlatform.MainView = new MainView((App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>());
            singleViewPlatform.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
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
                    services.AddSingleton<MainView>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<IMpcService, MpcService>();
                    services.AddTransient<IBinaryDownloader, BinaryDownloader>();

                    services.AddSingleton<QueuePage>(); 
                    services.AddSingleton<SearchPage>(); 
                    services.AddSingleton<LibraryPage>(); 
                    services.AddSingleton<PlaylistsPage>(); 
                    services.AddSingleton<PlaylistItemPage>(); 
                    services.AddSingleton<AlbumPage>();
                    services.AddSingleton<ArtistPage>();
                    services.AddSingleton<SettingsPage>();
                })
                .Build();
    }

    private static readonly StringBuilder Errortxt = new();
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
