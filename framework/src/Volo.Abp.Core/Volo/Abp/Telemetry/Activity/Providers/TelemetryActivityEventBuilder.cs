using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class TelemetryActivityEventBuilder : ITelemetryActivityEventBuilder, IScopedDependency
{
    private readonly IEnumerable<ITelemetryActivityEventEnricher> _activityDataEnrichers;

    public TelemetryActivityEventBuilder(IEnumerable<ITelemetryActivityEventEnricher> activityDataEnrichers)
    {
        _activityDataEnrichers = activityDataEnrichers;
    }

    public virtual async Task<ActivityEvent?> BuildAsync(ActivityContext context)
    {
        var enricherNodes = BuildTree(_activityDataEnrichers);
        await ExecuteEnrichersAsync(enricherNodes, context);
        
        if (context.IsTerminated)
        {
            return null;
        }
        return context.Current;
    }

    private async Task ExecuteEnrichersAsync(
        IEnumerable<EnricherNode> nodes,
        ActivityContext context)
    {
        async Task ExecuteEnricherChainAsync(IEnumerable<EnricherNode> currentNodes)
        {
            foreach (var node in currentNodes)
            {
                if (context.IsTerminated)
                {
                    return;
                }

                var enricher = node.Enricher;

                if (!await enricher.CanExecuteAsync(context))
                {
                    continue;
                }

                var enricherResult = await enricher.EnrichAsync(context);

                if (enricherResult != null)
                {
                    foreach (var kv in enricherResult)
                    {
                        context.Current[kv.Key] = kv.Value;
                    }
                }

                if (!context.IsCancelled)
                {
                    await ExecuteEnricherChainAsync(node.Children);
                }
                
                context.ResetCancel();
            }
        }

        await ExecuteEnricherChainAsync(nodes);
    }
    
    private static List<EnricherNode> BuildTree(IEnumerable<ITelemetryActivityEventEnricher> enrichers)
    {
        var enricherList = enrichers.ToList();
        var nodeMap = enricherList.ToDictionary(e => e.GetType(), e => new EnricherNode(e));

        foreach (var enricher in enricherList)
        {
            if (enricher.DependsOn != null)
            {
                if (nodeMap.TryGetValue(enricher.DependsOn, out var parentNode))
                {
                    parentNode.Children.Add(nodeMap[enricher.GetType()]);
                }
            }
        }

        var roots = enricherList
            .Where(e => e.DependsOn == null)
            .OrderByDescending(e => e.IsFirstRun)
            .Select(e => nodeMap[e.GetType()])
            .ToList();

        return roots;
    }
}