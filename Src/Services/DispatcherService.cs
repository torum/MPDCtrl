using CommunityToolkit.WinUI;
using MPDCtrl.Services.Contracts;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MPDCtrl.Services;

#pragma warning disable IDE0290 // Use primary constructor

public sealed class DispatcherService : IDispatcherService
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _queue;
    public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue => _queue;

    public DispatcherService(Microsoft.UI.Dispatching.DispatcherQueue queue)
    {
        _queue = queue;
    }

    //public bool TryEnqueue(Action action) => _queue.TryEnqueue(() => action());
    public bool TryEnqueue(Action action)
    {
        if (_queue is null) return false;

        try
        {
            return _queue.TryEnqueue(() =>
            {
                try
                {
                    action();
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Dispatcher or WinRT object invalid: swallow/log and avoid rethrowing.
                    Debug.WriteLine("DispatcherService System.Runtime.InteropServices.COMException");
                }
                catch (ObjectDisposedException)
                {
                    // Queue or UI object disposed: swallow/log.
                    Debug.WriteLine("DispatcherService ObjectDisposedException");
                }
                catch (Exception ex)
                {
                    _ = ex;
                    Debug.WriteLine($"DispatcherService Exception: {ex}");
                    (Microsoft.UI.Xaml.Application.Current as App)?.AppendErrorLog("Exception @TryEnqueue in DispatcherService", $"{ex.Message} {Environment.NewLine}StackTrace: {ex.StackTrace}, Source: {ex.Source}");
                    (Microsoft.UI.Xaml.Application.Current as App)?.SaveErrorLog();

                    // TODO: Not much help if we re-throw here. It will be caught in App_UnhandledException and crash the app. 
                    //throw;
                }
            });
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }


    // Awaitable action
    public Task EnqueueAsync(Action action) => _queue.EnqueueAsync(action);

    // Awaitable function that returns a value (e.g., getting text from a TextBox)
    public Task<T> EnqueueAsync<T>(Func<T> func) => _queue.EnqueueAsync(func);

    // Awaitable async function (e.g., showing a ContentDialog)
    public Task EnqueueAsync(Func<Task> func) => _queue.EnqueueAsync(func);

}
