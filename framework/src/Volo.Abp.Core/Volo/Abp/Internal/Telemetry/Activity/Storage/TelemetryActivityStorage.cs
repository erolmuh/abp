using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Internal.Telemetry.Activity.Contracts;
using Volo.Abp.Internal.Telemetry.Constants;
using Volo.Abp.Internal.Telemetry.Helpers;

namespace Volo.Abp.Internal.Telemetry.Activity.Storage;

public class TelemetryActivityStorage : ITelemetryActivityStorage, ISingletonDependency
{
    private readonly TelemetryPeriod _telemetryPeriod;

    private readonly static JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private TelemetryActivityStorageState State { get; }

    public TelemetryActivityStorage()
    {
        CreateDirectoryIfNotExist();
        
        State = LoadStateFromFile();
        
        _telemetryPeriod = new TelemetryPeriod();
    }

    public void SaveActivity(ActivityEvent activityEvent)
    {
        State.Activities.Add(activityEvent);

        if (activityEvent.ActivityName == ActivityNameConsts.AbpStudioClose)
        {
            State.SessionId = null;
        }

        if (activityEvent.HasDeviceInfo())
        {
            State.LastDeviceInfoAddTime = DateTimeOffset.UtcNow;
        }

        if (activityEvent.HasSolutionInfo())
        {
            var solutionId = (Guid)activityEvent[ActivityPropertyNames.SolutionId];
            State.Solutions[solutionId] = DateTimeOffset.UtcNow;
        }

        if (activityEvent.HasProjectInfo())
        {
            var projectId = (Guid)activityEvent[ActivityPropertyNames.ProjectId];
            State.Projects[projectId] = DateTimeOffset.UtcNow;
        }

        SaveState();
    }

    public List<ActivityEvent> GetActivities()
    {
        return State.Activities;
    }

    public Guid InitializeOrGetSession()
    {
        if (State.SessionId.HasValue)
        {
            return State.SessionId.Value;
        }

        State.SessionId = Guid.NewGuid();
        SaveState();

        return State.SessionId.Value;
    }

    public void MarkActivitiesAsSent()
    {
        State.ActivitySendTime = DateTimeOffset.UtcNow;
        State.Activities.Clear();
        SaveState();
    }

    public bool ShouldAddDeviceInfo()
    {
        return State.LastDeviceInfoAddTime is null ||
               DateTimeOffset.UtcNow - State.LastDeviceInfoAddTime > _telemetryPeriod.InformationSendPeriod;
    }

    public bool ShouldAddSolutionInformation(Guid solutionId)
    {
        return !State.Solutions.TryGetValue(solutionId, out var lastSend) ||
               DateTimeOffset.UtcNow - lastSend > _telemetryPeriod.InformationSendPeriod;
    }

    public bool ShouldAddProjectInfo(Guid projectId)
    {
        return !State.Projects.TryGetValue(projectId, out var lastSend) ||
               DateTimeOffset.UtcNow - lastSend > _telemetryPeriod.InformationSendPeriod;
    }

    public bool ShouldSendActivities()
    {
        return State.ActivitySendTime is null ||
               DateTimeOffset.UtcNow - State.ActivitySendTime > _telemetryPeriod.ActivitySendPeriod;
    }


    private TelemetryActivityStorageState LoadStateFromFile()
    {
        try
        {
            if (!File.Exists(TelemetryPaths.ActivityStorage))
            {
                return new TelemetryActivityStorageState();
            }

            return MutexExecutor.Execute(() =>
            {
                using var stream = new FileStream(TelemetryPaths.ActivityStorage, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var encryptedJson = reader.ReadToEnd();

                if (encryptedJson.IsNullOrEmpty())
                {
                    return new TelemetryActivityStorageState();
                }

                var json = Cryptography.Decrypt(encryptedJson);

                return JsonSerializer.Deserialize<TelemetryActivityStorageState>(json, JsonSerializerOptions);
            }) ?? new TelemetryActivityStorageState();
        }
        catch
        {
            return new TelemetryActivityStorageState();
        }
    }

    private void SaveState()
    {
        try
        {
            var json = JsonSerializer.Serialize(State, JsonSerializerOptions);
            var encryptedJson = Cryptography.Encrypt(json);
            File.WriteAllText(TelemetryPaths.ActivityStorage, encryptedJson, Encoding.UTF8);
        }
        catch
        {
            // Ignored 
        }
    }

    private static void CreateDirectoryIfNotExist()
    {
        try
        {
            var storageDirectory = Path.GetDirectoryName(TelemetryPaths.ActivityStorage)!;

            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }
        }
        catch
        {
            // Ignored 
        }
    }
}