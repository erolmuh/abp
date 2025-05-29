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
    private const int MaxFileRetries = 5;
    private const int RetryDelayMs = 100;
    private static readonly TimeSpan InfoExpirationPeriod = TimeSpan.FromDays(7);
    
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

    public async Task<(Guid SessionId, bool IsFirstSession)> GetOrCreateSessionAsync()
    {
        var isFirstSession = !File.Exists(TelemetryPaths.ActivityStorage);
        
        var state = await GetStateAsync();
        
        if (state.SessionId is null)
        {
            state.SessionId = Guid.NewGuid();
            await SaveAsync();
        }
        
        return (state.SessionId.Value, isFirstSession);
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

    public async Task MarkApplicationInfoAsAddedAsync(Guid applicationId)
    {
        var state = await GetStateAsync();
        state.ApplicationInfos[applicationId] = DateTimeOffset.UtcNow;
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

    private async Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid solutionId)
    {
        var state = await GetStateAsync();
        return state.Solutions.TryGetValue(solutionId, out var date) ? date : null;
    }

    private async Task<DateTimeOffset?> GetLastApplicationInfoSendTimeAsync(Guid applicationId)
    {
        var state = await GetStateAsync();
        return state.ApplicationInfos.TryGetValue(applicationId, out var date) ? date : null;
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
        for (int i = 0; i < MaxFileRetries; i++)
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

    private static bool ShouldAddInfo(DateTimeOffset? lastSend)
    {
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > InfoExpirationPeriod;
    }
}