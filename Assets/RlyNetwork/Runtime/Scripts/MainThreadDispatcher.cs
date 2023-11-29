#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    static MainThreadDispatcher? instance;

    static readonly ConcurrentQueue<Action> queue = new();

    static int mainThreadId;

    public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

    public static Task<T> RunAsync<T>(Func<T> callback)
    {
        if (IsMainThread)
            return RunAsyncNow(callback);

        var tcs = new TaskCompletionSource<T>();
        queue.Enqueue(() =>
        {
            try
            {
                tcs.SetResult(callback());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    void Update()
    {
        if (queue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);

            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    static Task<T> RunAsyncNow<T>(Func<T> callback)
    {
        try
        {
            return Task.FromResult(callback());
        }
        catch (Exception e)
        {
            return Task.FromException<T>(e);
        }
    }
}