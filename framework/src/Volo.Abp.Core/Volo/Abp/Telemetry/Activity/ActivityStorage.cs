using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Shared;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityStorage : IActivityStorage, ISingletonDependency
{
    private ActivityStorageState? _cachedState;


    public async Task BufferActivityAsync(ActivityData activityData)
    {
        var state = await GetStateAsync();
        state.Activities.Add(activityData);
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


    public async Task MarkApplicationInfoAsSentAsync(Guid applicationId)
    {
        var state = await GetStateAsync();
        state.ApplicationInfos[applicationId] = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    public async Task<DateTimeOffset?> GetApplicationInfoLastActivitySendTimeAsync(Guid applicationId)
    {
        var state = await GetStateAsync();
        if (state.ApplicationInfos.TryGetValue(applicationId, out var lastActivitySendTime))
        {
            return lastActivitySendTime;
        }

        return null;
    }

    public async Task<DateTimeOffset?> GetLastActivitySendTimeAsync()
    {
        var state = await GetStateAsync();
        return state.ActivitySendTime;
    }



    public async Task<(bool isFirstSession, Guid sessionId)> GetOrCreateSessionInfoAsync()
    {
        var state = await GetStateAsync();
        state.IsFirstSession ??= true;
        state.SessionId ??= Guid.NewGuid();
        await SaveAsync();
        return (state.IsFirstSession.Value, state.SessionId.Value);
    }

    public async Task MarkActivitiesAsSentAsync()
    {
        var state = await GetStateAsync();
        state.ActivitySendTime = DateTimeOffset.UtcNow;
        state.Activities.Clear();
        state.SessionId = null;
        state.IsFirstSession = false;

        await SaveAsync();
    }

    public async Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid id)
    {
        var state = await GetStateAsync();

        if (state.Solutions.TryGetValue(id, out var date))
        {
            return date;
        }

        return null;
    }

    public async Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync()
    {
        var state = await GetStateAsync();
        return state.LastDeviceInfoSendTime;
    }

    public async Task MarkSolutionInfoAsSentAsync(Guid id)
    {
        var state = await GetStateAsync();
        state.Solutions[id] = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    public async Task MarkDeviceInfoAsSentAsync()
    {
        var state = await GetStateAsync();
        state.LastDeviceInfoSendTime = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    private async Task<ActivityStorageState> GetStateAsync()
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
            return JsonSerializer.Deserialize<ActivityStorageState?>(json) ?? new ActivityStorageState();
        });
        return _cachedState;
    }

    private async Task<TResult> WithExclusiveFileLockAsync<TResult>(Func<FileStream, Task<TResult>> action)
    {
        const int maxRetries = 5;
        const int retryDelayMs = 100;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var stream = new FileStream(
                    AbpTelemetryPaths.ActivityStorage,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None
                );

                return await action(stream);
            }
            catch (IOException)
            {
                if (i == maxRetries - 1)
                {
                    throw;
                }

                await Task.Delay(retryDelayMs);
            }
        }

        throw new IOException("Unable to acquire file lock for ActivityStorage.");
    }

    private Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_cachedState ?? new ActivityStorageState(),
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        File.WriteAllText(AbpTelemetryPaths.ActivityStorage, json, Encoding.UTF8);
        return Task.CompletedTask;
    }


    private async Task EnsureFileExistsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(AbpTelemetryPaths.ActivityStorage);

            if (!Directory.Exists(directory) && !directory.IsNullOrEmpty())
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(AbpTelemetryPaths.ActivityStorage))
            {
                var json = JsonSerializer.Serialize(_cachedState ?? new ActivityStorageState(),
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AbpTelemetryPaths.ActivityStorage, json, Encoding.UTF8);
            }
        }
        catch
        {
            //ignored
        }
    }
}