using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.Shared;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.Activity;

public interface IActivityStorage
{
    Task<DateTimeOffset?> GetLastActivitySendTimeAsync();
    Task<(bool isFirstSession, Guid sessionId)> GetOrCreateSessionInfoAsync();
    Task MarkActivitiesAsSentAsync();
    Task MarkDeviceInfoAsSentAsync();
    Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid solutionId);
    Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync();
    Task BufferActivityAsync(ActivityData activityData);
    Task<List<ActivityData>> GetBufferedActivitiesAsync();
    Task EndSessionAsync();
    Task<bool> ShouldAddDeviceInfoAsync();
    Task<bool> ShouldAddSolutionInformation(Guid solutionId);
}