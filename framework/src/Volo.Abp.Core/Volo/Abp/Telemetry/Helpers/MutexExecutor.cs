using System;
using System.IO;
using System.Threading;

namespace Volo.Abp.Telemetry.Helpers;

static internal class MutexExecutor
{
    private const int MaxRetries = 5;
    private const int Timeout = 3000;
    private const string MutexName = "Global\\TelemetryActivityStorage";

    public static TResult? Execute<TResult>(Func<TResult> action)
    {
        using var mutex = new Mutex(false, MutexName);
        return ExecuteWithRetries(mutex, action);
    }

    private static TResult? ExecuteWithRetries<TResult>(Mutex mutex, Func<TResult> action)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            if (TryExecuteAction(mutex, action) is { } result)
            {
                return result;
            }

            if (attempt == MaxRetries)
            {
                return default;
            }
        }

        return default;
    }

    private static TResult? TryExecuteAction<TResult>(Mutex mutex, Func<TResult> action)
    {
        if (mutex.WaitOne(Timeout))
        {
            return default;
        }

        try
        {
            return action();
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