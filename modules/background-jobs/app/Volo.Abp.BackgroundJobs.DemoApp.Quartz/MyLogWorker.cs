using System;
using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;

namespace Volo.Abp.BackgroundJobs.DemoApp.Quartz;

public class MyLogWorker : QuartzBackgroundWorkerBase
{
    public MyLogWorker()
    {
        JobDetail = JobBuilder.Create<MyLogWorker>().WithIdentity(nameof(MyLogWorker)).Build();
        Trigger = TriggerBuilder.Create().WithIdentity(nameof(MyLogWorker)).StartAt(DateTimeOffset.Now.AddSeconds(10)).Build();
    }

    public override Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine("Executed MyLogWorker..!");
        return Task.CompletedTask;
    }
}

public class MyLogWorker2 : QuartzBackgroundWorkerBase
{
    public MyLogWorker2()
    {
        JobDetail = JobBuilder.Create<MyLogWorker2>().WithIdentity(nameof(MyLogWorker2)).Build();
        Trigger = TriggerBuilder.Create().WithIdentity(nameof(MyLogWorker2)).StartAt(DateTimeOffset.Now.AddSeconds(10)).Build();
    }

    public override Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine("Executed MyLogWorker2..!");
        return Task.CompletedTask;
    }
}
