using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace Volo.Abp.BackgroundJobs.DemoApp;

public class PassiveUserCheckerWorker : AsyncPeriodicBackgroundWorkerBase
{
    public PassiveUserCheckerWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 3000; //3 seconds
    }

    protected override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Console.WriteLine("Starting: Setting status of inactive users...");
        Console.WriteLine("Completed: Setting status of inactive users...");
        return Task.CompletedTask;
    }
}
