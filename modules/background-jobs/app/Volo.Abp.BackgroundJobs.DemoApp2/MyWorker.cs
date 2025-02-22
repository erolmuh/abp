using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;

namespace Volo.Abp.BackgroundJobs.DemoApp2;

public class MyWorker : BackgroundWorkerBase
{
    public override Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("MyWorker started..!");
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("MyWorker stopped..!");
        return Task.CompletedTask;
    }
}
