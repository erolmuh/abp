using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Helpers;

public static class MutexExecutor
{
    private const int MaxRetries = 5;
    private const int RetryDelay = 100;
    private const int Timeout = 5000;

    public async static Task<TResult?> ExecuteAsync<TResult>(string mutexName, Func<Task<TResult>> action)
    {
        using var mutex = new Mutex(false, mutexName);
        return await ExecuteWithRetriesAsync(mutex, action);
    }

    private async static Task<TResult?> ExecuteWithRetriesAsync<TResult>(Mutex mutex, Func<Task<TResult>> action)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            if (await TryExecuteActionAsync(mutex, action) is { } result)
            {
                return result;
            }

            if (attempt == MaxRetries)
            {
                return default;
            }

            await Task.Delay(RetryDelay);
        }

        return default;
    }

    private async static Task<TResult?> TryExecuteActionAsync<TResult>(Mutex mutex, Func<Task<TResult>> action)
    {
        if (!await WaitAsync(mutex))
        {
            return default;
        }

        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is AbandonedMutexException or IOException)
        {
            return default;
        }
        finally
        {
            ReleaseSafely(mutex);
        }
    }

    private async static Task<bool> WaitAsync(Mutex mutex)
    {
        return await Task.Run(() => mutex.WaitOne(Timeout));
    }

    private static void ReleaseSafely(Mutex mutex)
    {
        try 
        { 
            mutex.ReleaseMutex(); 
        }
        catch
        {
            // ignored intentionally
        }
    }
}