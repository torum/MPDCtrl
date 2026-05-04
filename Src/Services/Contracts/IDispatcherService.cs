using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Services.Contracts
{
    public interface IDispatcherService
    {
        Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; }
        bool TryEnqueue(Action action);
        Task EnqueueAsync(Action action);
        Task<T> EnqueueAsync<T>(Func<T> function);
        Task EnqueueAsync(Func<Task> function);
    }
}
