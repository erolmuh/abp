using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Helpers;

namespace Volo.Abp.Telemetry.Activity.Storage;

public class TelemetryActivityStorage : ITelemetryActivityStorage, ISingletonDependency
{
    private const string MutexName = "Global\\TelemetryActivityStorage";
    private const string TestModeEnvironmentVariable = "ABP_TELEMETRY_TEST_MODE";
    private readonly TimeSpan _infoExpirationPeriod;
    private readonly TimeSpan _activitySendPeriod;
    private TelemetryActivityStorageState? _cachedState;
    private readonly static JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public TelemetryActivityStorage()
    {
        var isTestMode = IsTestModeEnabled();
        _infoExpirationPeriod = isTestMode ? TimeSpan.FromSeconds(15) : TimeSpan.FromDays(7);
        _activitySendPeriod = isTestMode ? TimeSpan.FromSeconds(5) : TimeSpan.FromDays(1);
    }

    public async Task BufferActivityAsync(ActivityEvent activityEvent)
    {
        await UpdateStateAsync(state =>
        {
            state.Activities.Insert(0, activityEvent);
            
            if (activityEvent.ActivityName == ActivityNameConsts.AbpStudioClose)
            {
                state.SessionId = null;
            }

            if (activityEvent.ContainsKey(ActivityPropertyNames.HasSolutionInfo))
            {
                state.Solutions[Guid.Parse(activityEvent[ActivityPropertyNames.SolutionId].ToString()!)] = DateTimeOffset.UtcNow;
            }
            
            if (activityEvent.ContainsKey(ActivityPropertyNames.HasProjectInfo))
            {
                state.Projects[Guid.Parse(activityEvent[ActivityPropertyNames.ProjectId].ToString()!)] = DateTimeOffset.UtcNow;
            }

            if (activityEvent.ContainsKey(ActivityPropertyNames.HasDeviceInfo))
            {
                state.LastDeviceInfoAddTime = DateTimeOffset.UtcNow;
            }
        });
    }

    public async Task<List<ActivityEvent>> GetBufferedActivitiesAsync()
    {
        var state = await GetStateAsync();
        return state.Activities;
    }

    public async Task<Guid> InitializeOrGetSessionAsync()
    {
        var state = await GetStateAsync();

        if (state.SessionId.HasValue)
        {
            return state.SessionId.Value;
        }

        await UpdateStateAsync(s =>
        {
            s.SessionId = Guid.NewGuid();
        });
        return state.SessionId!.Value;
    }

    public async Task MarkActivitiesAsSentAsync()
    {
        await UpdateStateAsync(state =>
        {
            state.ActivitySendTime = DateTimeOffset.UtcNow;
            state.Activities.Clear();
        });
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

    public virtual async Task<bool> ShouldAddProjectInfoAsync(Guid projectId)
    {
        var lastSend = await GetLastProjectInfoSendTimeAsync(projectId);
        return ShouldAddInfo(lastSend);
    }

    public virtual async Task<bool> ShouldSendActivitiesAsync()
    {
        var lastActivitySendTime = await GetLastActivitySendTimeAsync();
        return lastActivitySendTime is null || DateTimeOffset.UtcNow - lastActivitySendTime > _activitySendPeriod;
    }

    private static bool IsTestModeEnabled()
    {
        var testModeVariable = Environment.GetEnvironmentVariable(TestModeEnvironmentVariable, EnvironmentVariableTarget.User);
        return bool.TryParse(testModeVariable, out var isTestMode) && isTestMode;
    }

    private async Task UpdateStateAsync(Action<TelemetryActivityStorageState> modifyAction)
    {
        var state = await GetStateAsync();
        modifyAction(state);
        Save();
    }

    private async Task<DateTimeOffset?> GetLastActivitySendTimeAsync()
    {
        var state = await GetStateAsync();
        return state.ActivitySendTime;
    }

    private async Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid solutionId)
    {
        var state = await GetStateAsync();
        return state.Solutions.TryGetValue(solutionId, out var date) ? date : null;
    }

    private async Task<DateTimeOffset?> GetLastProjectInfoSendTimeAsync(Guid applicationId)
    {
        var state = await GetStateAsync();
        return state.Projects.TryGetValue(applicationId, out var date) ? date : null;
    }

    private async Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync()
    {
        var state = await GetStateAsync();
        return state.LastDeviceInfoAddTime;
    }

    private bool ShouldAddInfo(DateTimeOffset? lastSend)
    {
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > _infoExpirationPeriod;
    }

    private async ValueTask<TelemetryActivityStorageState> GetStateAsync()
    {
        if (_cachedState != null)
        {
            return _cachedState;
        }

        EnsureStorageExists();
        
        _cachedState = await WithExclusiveFileLockAsync(async stream =>
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var encryptedJson = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(encryptedJson))
            {
                return new TelemetryActivityStorageState();
            }

            var json = Cryptography.Decrypt(encryptedJson);
            return JsonSerializer.Deserialize<TelemetryActivityStorageState>(json, JsonSerializerOptions) 
                   ?? new TelemetryActivityStorageState();
        }) ?? new TelemetryActivityStorageState();
    
        return _cachedState;
    }

    private void Save()
    {
        var state = _cachedState ?? new TelemetryActivityStorageState();
        var json = JsonSerializer.Serialize(state, JsonSerializerOptions);
        var encryptedJson = Cryptography.Encrypt(json);

        File.WriteAllText(TelemetryPaths.ActivityStorage, encryptedJson, Encoding.UTF8);
    }

    private void EnsureStorageExists()
    {
        try
        {
            var storageDirectory = Path.GetDirectoryName(TelemetryPaths.ActivityStorage)!;
            
            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }
            
            if (!File.Exists(TelemetryPaths.ActivityStorage))
            {
                var initialState = _cachedState ?? new TelemetryActivityStorageState();
                var json = JsonSerializer.Serialize(initialState, JsonSerializerOptions);
                var encryptedJson = Cryptography.Encrypt(json);
                File.WriteAllText(TelemetryPaths.ActivityStorage, encryptedJson, Encoding.UTF8);
            }
        }
        catch
        {
            // Ignored 
        }
    }

    private async Task<TResult?> WithExclusiveFileLockAsync<TResult>(Func<FileStream, Task<TResult>> action)
    {
        return await MutexExecutor.ExecuteAsync(MutexName, async () =>
        {
             using var stream = new FileStream(
                TelemetryPaths.ActivityStorage,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read
            );
            return await action(stream);
        });
    }
}