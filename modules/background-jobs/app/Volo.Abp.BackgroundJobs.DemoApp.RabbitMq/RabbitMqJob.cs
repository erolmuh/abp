using System;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.BackgroundJobs.DemoApp.RabbitMq
{
    public class RabbitMqJob : BackgroundJob<RabbitMqJobArgs>, ITransientDependency
    {
        public override void Execute(RabbitMqJobArgs args)
        {
            Console.WriteLine($"[RabbitMqJob] Started: {args.Value}");
        }
    }
}
