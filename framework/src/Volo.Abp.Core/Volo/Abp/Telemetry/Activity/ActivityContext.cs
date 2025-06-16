using System;
using System.Collections.Generic;
using System.Threading;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityContext
{
    private ActivityContext(ActivityEvent current)
    {
        Current = current;
    }

    public ActivityEvent Current { get; }
    public Dictionary<string, object> ExtraProperties { get; } = new();
    public bool IsTerminated { get; private set; }
    public bool IsCancelled { get; private set; }

    public CancellationToken CancellationToken { get; set; }


    public static ActivityContext Create(string activityName, string? details = null,
        Action<Dictionary<string, object>>? additionalProperties = null)
    {
        var activity = new ActivityEvent(activityName, details);
        if (additionalProperties is not null)
        {
            activity.AdditionalProperties = new Dictionary<string, object>();
            additionalProperties.Invoke(activity.AdditionalProperties);
        }

        return new ActivityContext(activity);
    }

    public Guid? ProjectId {
        get {
            if (!Current.TryGetValue(ActivityPropertyNames.ProjectId, out var projectId))
            {
                return null;
            }

            if (Guid.TryParse(projectId.ToString(), out var projectIdGuid))
            {
                return projectIdGuid;
            }

            return null;
        }
    }

    public Guid? SolutionId {
        get {
            if (!Current.TryGetValue(ActivityPropertyNames.SolutionId, out var solutionId))
            {
                return null;
            }

            if (Guid.TryParse(solutionId!.ToString(), out var solutionIdGuid))
            {
                return solutionIdGuid;
            }

            return null;
        }
    }

    public SessionType? SessionType {
        get {
            if (Current.TryGetValue(ActivityPropertyNames.SessionType, out var sessionTypeObj) &&
                Enum.TryParse<SessionType>(sessionTypeObj?.ToString(), out var sessionType))
            {
                return sessionType;
            }

            return null;
        }
    }

    public string? SolutionPath => ExtraProperties.TryGetValue(ActivityPropertyNames.SolutionPath, out var solutionPath)
        ? solutionPath?.ToString()
        : null;


    public string? DeviceId => Current.TryGetValue(ActivityPropertyNames.DeviceId, out var deviceId) ? deviceId?.ToString() : null;

    public void Terminate()
    {
        IsTerminated = true;
    }

    public void Cancel()
    {
        IsCancelled = true;
    }

    public void ResetCancel()
    {
        IsCancelled = false;
    }
}