using System;

namespace Volo.Abp.Internal.Telemetry.Activity.Contracts;

public interface IHasParentTelemetryActivityEventEnricher
{
    Type ParentType { get; }
}