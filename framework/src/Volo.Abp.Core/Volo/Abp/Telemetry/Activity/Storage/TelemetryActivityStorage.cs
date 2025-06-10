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

    public TelemetryActivityStorage()
    {
        var isTestMode = IsTestModeEnabled();
        _infoExpirationPeriod = isTestMode ? TimeSpan.FromSeconds(15) : TimeSpan.FromDays(7);
        _activitySendPeriod = isTestMode ? TimeSpan.FromSeconds(5) : TimeSpan.FromDays(1);
    }

    public async Task BufferActivityAsync(ActivityData activityData)
    {
        await ModifyStateAsync(state =>
        {
            state.Activities.Insert(0, activityData);
        });
    }

    public async Task<List<ActivityData>> GetBufferedActivitiesAsync()
    {
        var state = await GetStateAsync();
        return state.Activities;
    }

    public async Task EndSessionAsync()
    {
        await ModifyStateAsync(state =>
        {
            state.SessionId = null;
        });
    }

    public async Task<Guid> GetOrCreateSessionAsync()
    {
        var state = await GetStateAsync();

        if (state.SessionId.HasValue)
        {
            return state.SessionId.Value;
        }

        await ModifyStateAsync(s =>
        {
            s.SessionId = Guid.NewGuid();
        });
        return state.SessionId!.Value;
    }

    public async Task MarkActivitiesAsSentAsync()
    {
        await ModifyStateAsync(state =>
        {
            state.ActivitySendTime = DateTimeOffset.UtcNow;
            state.Activities.Clear();
        });
    }

    public async Task MarkSolutionInfoAsAddedAsync(Guid solutionId)
    {
        await ModifyStateAsync(state =>
        {
            state.Solutions[solutionId] = DateTimeOffset.UtcNow;
        });
    }

    public async Task MarkApplicationInfoAsAddedAsync(Guid applicationId)
    {
        await ModifyStateAsync(state =>
        {
            state.Applications[applicationId] = DateTimeOffset.UtcNow;
        });
    }

    public async Task MarkDeviceInfoAsAddedAsync()
    {
        await ModifyStateAsync(state =>
        {
            state.LastDeviceInfoAddTime = DateTimeOffset.UtcNow;
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

    public virtual async Task<bool> ShouldAddApplicationInfoAsync(Guid applicationId)
    {
        var lastSend = await GetLastApplicationInfoSendTimeAsync(applicationId);
        return ShouldAddInfo(lastSend);
    }

    public virtual async Task<bool> ShouldSendActivitiesAsync()
    {
        var lastActivitySendTime = await GetLastActivitySendTimeAsync();
        return lastActivitySendTime is null || DateTimeOffset.UtcNow - lastActivitySendTime > _activitySendPeriod;
    }

    private static bool IsTestModeEnabled()
    {
        var testModeVariable =
            Environment.GetEnvironmentVariable(TestModeEnvironmentVariable, EnvironmentVariableTarget.User);
        return bool.TryParse(testModeVariable, out var isTestMode) && isTestMode;
    }

    private async Task ModifyStateAsync(Action<TelemetryActivityStorageState> modifyAction)
    {
        var state = await GetStateAsync();
        modifyAction(state);
        await SaveAsync();
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

    private bool ShouldAddInfo(DateTimeOffset? lastSend)
    {
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > _infoExpirationPeriod;
    }

    private async Task<TelemetryActivityStorageState> GetStateAsync()
    {
        if (_cachedState != null)
        {
            return _cachedState;
        }

        EnsureFileExists();
        _cachedState = await LoadStateFromFileAsync() ?? new TelemetryActivityStorageState();
        return _cachedState;
    }

    private async Task<TelemetryActivityStorageState?> LoadStateFromFileAsync()
    {
        return await WithExclusiveFileLockAsync(async stream =>
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var encryptedJson = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(encryptedJson))
            {
                return new TelemetryActivityStorageState();
            }

            var json = EncryptionHelper.Decrypt(encryptedJson);
            return JsonSerializer.Deserialize<TelemetryActivityStorageState?>(json, GetJsonSerializerOptions())
                   ?? new TelemetryActivityStorageState();
        });
    }

    private Task SaveAsync()
    {
        var state = _cachedState ?? new TelemetryActivityStorageState();
        var json = JsonSerializer.Serialize(state, GetJsonSerializerOptions());
        var encryptedJson = EncryptionHelper.Encrypt(json);

        File.WriteAllText(TelemetryPaths.ActivityStorage, encryptedJson, Encoding.UTF8);
        return Task.CompletedTask;
    }

    private void EnsureFileExists()
    {
        try
        {
            EnsureDirectoryExists();

            if (!File.Exists(TelemetryPaths.ActivityStorage))
            {
                CreateInitialFile();
            }
        }
        catch
        {
            // Ignored intentionally
        }
    }

    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(TelemetryPaths.ActivityStorage);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }
    }

    private void CreateInitialFile()
    {
        var initialState = _cachedState ?? new TelemetryActivityStorageState();
        var json = JsonSerializer.Serialize(initialState, GetJsonSerializerOptions());
        var encryptedJson = EncryptionHelper.Encrypt(json);
        File.WriteAllText(TelemetryPaths.ActivityStorage, encryptedJson, Encoding.UTF8);
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


    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}