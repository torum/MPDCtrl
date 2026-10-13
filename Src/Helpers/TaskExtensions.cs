using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Helpers;

internal static class TaskExtensions
{
    /// <summary>
    /// Observes a task that nobody awaits. Cancellation is ignored; other exceptions are logged.
    /// </summary>
    public static async Task SafeAsync(this Task task, string origin)
    {
        try
        {
            await task.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown or when a newer request supersedes this one.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception @{origin}: {ex}");
            (App.Current as App)?.AppendErrorLog(origin, ex.ToString());
            (App.Current as App)?.SaveErrorLog();
        }
    }
}
