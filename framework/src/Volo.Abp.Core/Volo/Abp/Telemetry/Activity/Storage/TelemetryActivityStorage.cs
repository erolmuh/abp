using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Storage;

public class TelemetryActivityStorage : ITelemetryActivityStorage, ISingletonDependency
{
    private const int MaxFileRetries = 5;
    private const int RetryDelayMs = 100;
    private const int MutexTimeoutMs = 5000; 

    private readonly TelemetryActivityStorageOptions _options;
    private TelemetryActivityStorageState? _cachedState;
    private static readonly string MutexName = "Global\\TelemetryActivityStorage";

    public TelemetryActivityStorage(IOptions<TelemetryActivityStorageOptions> options)
    {
        _options = options?.Value ?? new TelemetryActivityStorageOptions();

    }

    public async Task BufferActivityAsync(ActivityData activityData)
    {
        var state = await GetStateAsync();
        state.Activities.Insert(0, activityData);
        await SaveAsync();
    }

    public async Task<List<ActivityData>> GetBufferedActivitiesAsync()
    {
        var state = await GetStateAsync();
        return state.Activities.OrderBy(x => x.Time).ToList();
    }

    public async Task EndSessionAsync()
    {
        var state = await GetStateAsync();
        state.SessionId = null;
        await SaveAsync();
    }

    public async Task<DateTimeOffset?> GetLastActivitySendTimeAsync()
    {
        var state = await GetStateAsync();
        return state.ActivitySendTime;
    }

    public async Task<Guid> GetOrCreateSessionAsync()
    {
        var state = await GetStateAsync();

        if (state.SessionId is null)
        {
            state.SessionId = Guid.NewGuid();
            await SaveAsync();
        }

        return state.SessionId.Value;
    }

    public async Task MarkActivitiesAsSentAsync()
    {
        var state = await GetStateAsync();
        state.ActivitySendTime = DateTimeOffset.UtcNow;
        state.Activities.Clear();
        await SaveAsync();
    }

    public async Task MarkSolutionInfoAsAddedAsync(Guid solutionId)
    {
        var state = await GetStateAsync();
        state.Solutions[solutionId] = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    public async Task MarkApplicationInfoAsAddedAsync(Guid applicationId)
    {
        var state = await GetStateAsync();
        state.Applications[applicationId] = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    public async Task MarkDeviceInfoAsAddedAsync()
    {
        var state = await GetStateAsync();
        state.LastDeviceInfoAddTime = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    public virtual async Task<bool> ShouldAddDeviceInfoAsync()
    {
        var lastSend = await GetLastDeviceInfoSendTimeAsync();
        return ShouldAddInfo(lastSend);
    }

    public virtual async Task<bool> ShouldAddSolutionInformation(Guid solutionId)
    {
        var lastSend = await GetLastSolutionInfoSendTimeAsync(solutionId);
        return ShouldAddInfo(lastSend);
    }

    public virtual async Task<bool> ShouldAddApplicationInfoAsync(Guid applicationId)
    {
        var lastSend = await GetLastApplicationInfoSendTimeAsync(applicationId);
        return ShouldAddInfo(lastSend);
    }

    public virtual async Task<bool> ShouldSendActivitiesAsync()
    {
        var lastActivitySendTime = await GetLastActivitySendTimeAsync();
        return lastActivitySendTime is null || DateTimeOffset.UtcNow - lastActivitySendTime > _options.ActivitySendPeriod;
    }

    private async Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid solutionId)
    {
        var state = await GetStateAsync();
        return state.Solutions.TryGetValue(solutionId, out var date) ? date : null;
    }

    private async Task<DateTimeOffset?> GetLastApplicationInfoSendTimeAsync(Guid applicationId)
    {
        var state = await GetStateAsync();
        return state.Applications.TryGetValue(applicationId, out var date) ? date : null;
    }

    private async Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync()
    {
        var state = await GetStateAsync();
        return state.LastDeviceInfoAddTime;
    }

    private async Task<TelemetryActivityStorageState> GetStateAsync()
    {
        if (_cachedState != null)
        {
            return _cachedState;
        }

        await EnsureFileExistsAsync();
        _cachedState = await WithExclusiveFileLockAsync(async stream =>
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<TelemetryActivityStorageState?>(json, GetJsonSerializerOptions())
                   ?? new TelemetryActivityStorageState();
        }) ?? new TelemetryActivityStorageState();
        return _cachedState;
    }

    private async Task<TResult?> WithExclusiveFileLockAsync<TResult>(Func<FileStream, Task<TResult>> action)
    {
        using var mutex = new Mutex(false, MutexName);

        for (int i = 0; i < MaxFileRetries; i++)
        {
            try
            {
                // Wait for mutex with timeout
                if (!mutex.WaitOne(MutexTimeoutMs))
                {
                    if (i == MaxFileRetries - 1)
                    {
                        return default;
                    }
                    await Task.Delay(RetryDelayMs);
                    continue;
                }

                try
                {
                    using var stream = new FileStream(
                        TelemetryPaths.ActivityStorage,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.Read // Allow read sharing since we have mutex protection
                    );

                    return await action(stream);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            catch (AbandonedMutexException)
            {
                // Previous process crashed, mutex is now owned by us
                try
                {
                    using var stream = new FileStream(
                        TelemetryPaths.ActivityStorage,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.Read
                    );

                    return await action(stream);
                }
                catch
                {
                    if (i == MaxFileRetries - 1)
                    {
                        return default;
                    }
                    await Task.Delay(RetryDelayMs);
                }
                finally
                {
                    try { mutex.ReleaseMutex(); } catch { }
                }
            }
            catch (IOException)
            {
                if (i == MaxFileRetries - 1)
                {
                    return default;
                }
                await Task.Delay(RetryDelayMs);
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    private Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_cachedState ?? new TelemetryActivityStorageState(), GetJsonSerializerOptions());
        File.WriteAllText(TelemetryPaths.ActivityStorage, json, Encoding.UTF8);
        return Task.CompletedTask;
    }

    private Task EnsureFileExistsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(TelemetryPaths.ActivityStorage);

            if (!Directory.Exists(directory) && !directory.IsNullOrEmpty())
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(TelemetryPaths.ActivityStorage))
            {
                var json = JsonSerializer.Serialize(_cachedState ?? new TelemetryActivityStorageState(), GetJsonSerializerOptions());
                File.WriteAllText(TelemetryPaths.ActivityStorage, json, Encoding.UTF8);
            }
        }
        catch
        {
            // Ignored intentionally
        }

        return Task.CompletedTask;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    private bool ShouldAddInfo(DateTimeOffset? lastSend)
    {
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > _options.InfoExpirationPeriod;
    }
}