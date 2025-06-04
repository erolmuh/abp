using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Helpers;

static internal class MutexExecutor
{
    private const int MaxRetries = 5;
    private const int RetryDelay = 100;
    private const int Timeout = 5000;

    public async static Task<TResult?> ExecuteAsync<TResult>(string mutexName, Func<Task<TResult>> action)
    {
        using var mutex = new Mutex(false, mutexName);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            if (!await WaitAsync(mutex))
            {
                if (attempt == MaxRetries)
                {
                    return default;
                }

                await Task.Delay(RetryDelay);
                continue;
            }

            try
            {
                return await action();
            }
            catch (AbandonedMutexException)
            {
                if (attempt == MaxRetries)
                {
                    return default;
                }

                await Task.Delay(RetryDelay);
            }
            catch (IOException)
            {
                if (attempt == MaxRetries)
                {
                    return default;
                }

                await Task.Delay(RetryDelay);
            }
            finally
            {
                ReleaseSafely(mutex);
            }
        }

        return default;
    }

    private async static Task<bool> WaitAsync(Mutex mutex)
    {
        return await Task.Run(() => mutex.WaitOne(Timeout));
    }

    private static void ReleaseSafely(Mutex mutex)
    {
        try { mutex.ReleaseMutex(); }
        catch
        {
            //ignored
        }
    }
}