using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Storage;

public class TelemetryActivityStorage : ITelemetryActivityStorage, ISingletonDependency
{
    private const int MaxFileRetries = 5;
    private const int RetryDelayMs = 100;
    private const int MutexTimeoutMs = 5000; 
    private const string MutexName = "Global\\TelemetryActivityStorage";
    private const string EncryptionKey = "AbpTelemetryStorageKey"; 

    private static TimeSpan _infoExpirationPeriod = TimeSpan.FromDays(7);
    private static TimeSpan _activitySendPeriod = TimeSpan.FromDays(1);
    
    private TelemetryActivityStorageState? _cachedState;
    

    public TelemetryActivityStorage()
    {
        var enableTelemetryTestModeVariable = Environment.GetEnvironmentVariable("ABP_TELEMETRY_TEST_MODE" , EnvironmentVariableTarget.User);
        if (bool.TryParse(enableTelemetryTestModeVariable, out var enableTelemetryTestMode) && enableTelemetryTestMode)
        {
            _infoExpirationPeriod = TimeSpan.FromMinutes(1);
            _activitySendPeriod = TimeSpan.FromSeconds(5);
        }
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

    private async Task<DateTimeOffset?> GetLastActivitySendTimeAsync()
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
        return lastActivitySendTime is null || DateTimeOffset.UtcNow - lastActivitySendTime > _activitySendPeriod;
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
            var encryptedJson = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(encryptedJson))
            {
                return new TelemetryActivityStorageState();
            }
            
            var json = Decrypt(encryptedJson);
            return JsonSerializer.Deserialize<TelemetryActivityStorageState?>(json, GetJsonSerializerOptions())
                   ?? new TelemetryActivityStorageState();
        }) ?? new TelemetryActivityStorageState();
        return _cachedState;
    }

    private async Task<TResult?> WithExclusiveFileLockAsync<TResult>(Func<FileStream, Task<TResult>> action)
    {
        return await RetryWithMutexAsync(async () =>
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

    private async Task<TResult?> RetryWithMutexAsync<TResult>(Func<Task<TResult>> operation)
    {
        using var mutex = new Mutex(false, MutexName);

        for (int attempt = 1; attempt <= MaxFileRetries; attempt++)
        {
            try
            {
                if (!await WaitForMutexAsync(mutex))
                {
                    if (attempt == MaxFileRetries)
                    {
                        return default;
                    }

                    await Task.Delay(RetryDelayMs);
                    continue;
                }

                try
                {
                    return await operation();
                }
                finally
                {
                    ReleaseMutexSafely(mutex);
                }
            }
            catch (AbandonedMutexException)
            {
                try
                {
                    return await operation();
                }
                catch
                {
                    if (attempt == MaxFileRetries)
                    {
                        return default;
                    }

                    await Task.Delay(RetryDelayMs);
                }
                finally
                {
                    ReleaseMutexSafely(mutex);
                }
            }
            catch (IOException)
            {
                if (attempt == MaxFileRetries)
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

    private async Task<bool> WaitForMutexAsync(Mutex mutex)
    {
        return await Task.Run(() => mutex.WaitOne(MutexTimeoutMs));
    }

    private static void ReleaseMutexSafely(Mutex mutex)
    {
        try
        {
            mutex.ReleaseMutex();
        }
        catch
        {
            // Ignore release errors
        }
    }

    private Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_cachedState ?? new TelemetryActivityStorageState(), GetJsonSerializerOptions());
        var encryptedJson = Encrypt(json);
        File.WriteAllText(TelemetryPaths.ActivityStorage, encryptedJson, Encoding.UTF8);
        return Task.CompletedTask;
    }

    private Task EnsureFileExistsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(TelemetryPaths.ActivityStorage);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            if (!File.Exists(TelemetryPaths.ActivityStorage))
            {
                var json = JsonSerializer.Serialize(_cachedState ?? new TelemetryActivityStorageState(), GetJsonSerializerOptions());
                var encryptedJson = Encrypt(json);
                File.WriteAllText(TelemetryPaths.ActivityStorage, encryptedJson, Encoding.UTF8);
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
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    private bool ShouldAddInfo(DateTimeOffset? lastSend)
    {
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > _infoExpirationPeriod;
    }

    private static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        using var sha256 = SHA256.Create();
        aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        var encryptor = aes.CreateEncryptor();
        var inputBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        using var sha256 = SHA256.Create();
        aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        var decryptor = aes.CreateDecryptor();
        var inputBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}