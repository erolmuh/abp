using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Cli;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Json;

namespace Activity;

public class ActivityStorage : IActivityStorage, ISingletonDependency
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly string _filePath;
    private ActivityStorageState? _cachedState;

    public ActivityStorage(IJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
        _filePath = Path.Combine(CliPaths.AbpRootPath, "activity-storage.json");
    }

    public async Task BufferActivityAsync(ActivityData activityData, CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        state.Activities.Add(activityData);
        await SaveAsync();
    }

    public async Task<List<ActivityData>> GetBufferedActivitiesAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        return state.Activities;
    }

    public async Task RemoveActivityAsync(string activityName, CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();

        var existing = state.Activities
            .FirstOrDefault(x => string.Equals(x.ActivityName, activityName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            state.Activities.Remove(existing);
            await SaveAsync();
        }
    }

    public async Task MarkApplicationInfoAsSentAsync(Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        state.ApplicationInfos[applicationId] = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    public async Task<DateTimeOffset?> GetApplicationInfoLastActivitySendTimeAsync(Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        if (state.ApplicationInfos.TryGetValue(applicationId, out var lastActivitySendTime))
        {
            return lastActivitySendTime;
        }

        return null;
    }

    public async Task<DateTimeOffset?> GetLastActivitySendTimeAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        return state.ActivitySendTime;
    }

    public async Task<Guid?> GetSessionIdAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        return state.SessionId ?? null;
    }

    public async Task<(bool isFirstSession, Guid sessionId)> GetOrCreateSessionInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        state.IsFirstSession ??= true;
        state.SessionId ??= Guid.NewGuid();
        await SaveAsync();
        return (state.IsFirstSession.Value, state.SessionId.Value);
    }

    public async Task SetSessionIdAsync(Guid sessionId, bool isFirstSession = false,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        state.SessionId = sessionId;
        state.IsFirstSession = isFirstSession;
        await SaveAsync();
    }

    public async Task MarkActivitiesAsSentAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        state.ActivitySendTime = DateTimeOffset.UtcNow;
        state.Activities.Clear();
        state.SessionId = null;

        await SaveAsync();
    }

    public async Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();

        if (state.Solutions.TryGetValue(id, out var date))
        {
            return date;
        }

        return null;
    }

    public async Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        return state.LastDeviceInfoSendTime;
    }

    public async Task MarkSolutionInfoAsSentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        state.Solutions[id] = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    public async Task MarkDeviceInfoAsSentAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync();
        state.LastDeviceInfoSendTime = DateTimeOffset.UtcNow;
        await SaveAsync();
    }

    private async Task<ActivityStorageState> GetStateAsync()
    {
        if (_cachedState != null)
            return _cachedState;

        await EnsureFileExistsAsync();
        _cachedState = await WithExclusiveFileLockAsync(async stream =>
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var json = await reader.ReadToEndAsync();
            return _jsonSerializer.Deserialize<ActivityStorageState?>(json) ?? new ActivityStorageState();
        });
        return _cachedState;
    }

    private async Task<TResult> WithExclusiveFileLockAsync<TResult>(Func<FileStream, Task<TResult>> action)
    {
        const int maxRetries = 10;
        const int retryDelayMs = 100;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await using var stream = new FileStream(
                    _filePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None
                );

                return await action(stream);
            }
            catch (IOException)
            {
                if (i == maxRetries - 1)
                    throw;

                await Task.Delay(retryDelayMs);
            }
        }

        throw new IOException("Unable to acquire file lock for ActivityStorage.");
    }

    private async Task SaveAsync()
    {
        var json = _jsonSerializer.Serialize(_cachedState ?? new ActivityStorageState(), indented: true);
        await File.WriteAllTextAsync(_filePath, json, Encoding.UTF8);
    }


    private async Task EnsureFileExistsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);

            if (!Directory.Exists(directory) && ! directory.IsNullOrEmpty())
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_filePath))
            {
                var json = _jsonSerializer.Serialize(new ActivityStorageState(), indented: true);
                await File.WriteAllTextAsync(_filePath, json, Encoding.UTF8);
            }
        }
        catch
        {
            //ignored
        }
    }
}