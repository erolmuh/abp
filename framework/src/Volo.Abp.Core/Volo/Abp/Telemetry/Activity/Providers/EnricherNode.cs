using System.Collections.Generic;
using Volo.Abp.Telemetry.Activity.Contracts;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class EnricherNode
{
    public ITelemetryActivityEventEnricher Enricher { get; set; }
    public List<EnricherNode> Children { get; set; } = new();

    public EnricherNode(ITelemetryActivityEventEnricher enricher)
    {
        Enricher = enricher;
    }
}