using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Volo.Abp.BackgroundWorkers.Hangfire;

namespace Volo.Abp.BackgroundJobs.DemoApp.HangFire;

public class MyLogWorker : HangfireBackgroundWorkerBase
{
    public MyLogWorker()
    {
        RecurringJobId = nameof(MyLogWorker);
        CronExpression = Cron.Minutely();
    }

    public override Task DoWorkAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Executed MyLogWorker..!");
        return Task.CompletedTask;
    }
}

public class MyLogWorker2 : HangfireBackgroundWorkerBase
{
    public MyLogWorker2()
    {
        RecurringJobId = nameof(MyLogWorker2);
        CronExpression = Cron.Minutely();
        Queue = "my_queue";
    }

    public override Task DoWorkAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Executed MyLogWorker2..!");
        return Task.CompletedTask;
    }
}
