using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Storage;

public class TelemetryActivityStorage : ITelemetryActivityStorage, ISingletonDependency
{
    private TelemetryActivityStorageState? _cachedState;

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


    public async Task<DateTimeOffset?> GetLastActivitySendTimeAsync()
    {
        var state = await GetStateAsync();
        return state.ActivitySendTime;
    }


    public async Task<Guid> GetOrCreateSessionInfoAsync()
    {
        var state = await GetStateAsync();
        state.SessionId ??= Guid.NewGuid();
        await SaveAsync();
        return state.SessionId.Value;
    }

    public async Task MarkActivitiesAsSentAsync()
    {
        var state = await GetStateAsync();
        state.ActivitySendTime = DateTimeOffset.UtcNow;
        state.Activities.Clear();
        state.SessionId = null;

        await SaveAsync();
    }

    public async Task MarkSolutionInfoAsAddedAsync(Guid solutionId)
    {
        var state = await GetStateAsync();
        state.Solutions[solutionId] = DateTimeOffset.UtcNow;
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
        return state.LastDeviceInfoAddTime;
    }

    public async Task MarkDeviceInfoAsAddedAsync()
    {
        var state = await GetStateAsync();
        state.LastDeviceInfoAddTime = DateTimeOffset.UtcNow;
        await SaveAsync();
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
            return JsonSerializer.Deserialize<TelemetryActivityStorageState?>(json,
                       new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ??
                   new TelemetryActivityStorageState();
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
                    TelemetryPaths.ActivityStorage,
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
        var json = JsonSerializer.Serialize(_cachedState ?? new TelemetryActivityStorageState(),
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
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
                var json = JsonSerializer.Serialize(_cachedState ?? new TelemetryActivityStorageState(),
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
                    });
                File.WriteAllText(TelemetryPaths.ActivityStorage, json, Encoding.UTF8);
            }
        }
        catch
        {
            //ignored
        }

        return Task.CompletedTask;
    }


    public virtual async Task<bool> ShouldAddDeviceInfoAsync()
    {
        var lastSend = await GetLastDeviceInfoSendTimeAsync();
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > TimeSpan.FromDays(7);
    }

    public virtual async Task<bool> ShouldAddSolutionInformation(Guid solutionId)
    {
        var lastSend = await GetLastSolutionInfoSendTimeAsync(solutionId);
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > TimeSpan.FromDays(7);
    }
}