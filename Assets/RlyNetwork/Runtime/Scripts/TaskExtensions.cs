#nullable enable

using System;
using System.Threading.Tasks;

public static class TaskExtensions
{
    public static Task ContinueWithOnMainThread(this Task task, Action<Task> continuationAction)
    {
        return task.ContinueWith(t => MainThreadDispatcher.RunAsync(() =>
        {
            continuationAction(t);
            return true;
        })).Unwrap();
    }

    public static Task ContinueWithOnMainThread<T>(this Task<T> task, Action<Task<T>> continuationAction)
    {
        return task.ContinueWith(t => MainThreadDispatcher.RunAsync(() =>
        {
            continuationAction(t);
            return true;
        })).Unwrap();
    }
}