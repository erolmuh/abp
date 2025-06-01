using System;
using System.IO;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry;

[ExposeServices(typeof(ITelemetrySessionProvider))]
public class TelemetryRuntimeSessionProvider : ITelemetrySessionProvider, ISingletonDependency
{
    public void AddSessionInfo(ActivityData activity)
    {
        activity[ActivityPropertyNames.SessionType] = SessionType.ApplicationRuntime;
        activity[ActivityPropertyNames.SessionId] = Guid.NewGuid();
        activity[ActivityPropertyNames.IsFirstSession] = !File.Exists(TelemetryPaths.ActivityStorage);
    }

   
}