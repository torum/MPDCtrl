using System;
using System.Threading.Tasks;

namespace MPDCtrl.Services.Contracts
{
    public interface IDispatcherService
    {
        Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; }
        bool TryEnqueue(Action action);
        Task EnqueueAsync(Action action);
        Task<T> EnqueueAsync<T>(Func<T> func);
        Task EnqueueAsync(Func<Task> func);
    }
}
