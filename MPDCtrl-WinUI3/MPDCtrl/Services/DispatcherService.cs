using CommunityToolkit.WinUI;
using MPDCtrl.Services.Contracts;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MPDCtrl.Services;

public class DispatcherService : IDispatcherService
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _queue;

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
                }
                catch (ObjectDisposedException)
                {
                    // Queue or UI object disposed: swallow/log.
                }
                catch (Exception ex)
                {
                    _ = ex;
                    Debug.WriteLine($"DispatcherService Exception: {ex}");
                    throw;
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
    public Task<T> EnqueueAsync<T>(Func<T> function) => _queue.EnqueueAsync(function);

    // Awaitable async function (e.g., showing a ContentDialog)
    public Task EnqueueAsync(Func<Task> function) => _queue.EnqueueAsync(function);

}
