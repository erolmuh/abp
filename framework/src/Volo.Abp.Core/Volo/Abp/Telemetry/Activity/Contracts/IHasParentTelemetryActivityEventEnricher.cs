using System;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface IHasParentTelemetryActivityEventEnricher
{
    Type Parent { get; }
}