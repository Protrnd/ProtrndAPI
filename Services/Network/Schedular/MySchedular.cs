using CRONJOBTesting.Models;
using Quartz;
using Quartz.Spi;

namespace CRONJOBTesting.Schedular
{
    public class MySchedular : IHostedService
    {
        private IScheduler Schedular { get; set; }
        private IJobFactory jobFactory;
        private JobMetadata jobMetadata;
        private ISchedulerFactory schedulerFactory;

        public MySchedular(IJobFactory jobFactory, JobMetadata jobMetadata, ISchedulerFactory schedulerFactory)
        {
            this.jobFactory = jobFactory;
            this.jobMetadata = jobMetadata;
            this.schedulerFactory = schedulerFactory;
        }

        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            Schedular = await schedulerFactory.GetScheduler();
            Schedular.JobFactory = jobFactory;
            IJobDetail jobDetail = CreateJob(jobMetadata);
            ITrigger trigger = CreateTrigger(jobMetadata);
            await Schedular.ScheduleJob(jobDetail, trigger, cancellationToken);
            await Schedular.Start(cancellationToken);
        }

        private ITrigger CreateTrigger(JobMetadata jobMetadata)
        {
            return TriggerBuilder.Create()
                .WithIdentity(jobMetadata.JobId.ToString())
                .WithCronSchedule(jobMetadata.CronExpression)
                .WithDescription(jobMetadata.JobName.ToString())
                .Build();
        }

        private IJobDetail CreateJob(JobMetadata jobMetadata)
        {
            return JobBuilder.Create(jobMetadata.JobType)
                .WithIdentity(jobMetadata.JobId.ToString())
                .WithDescription(jobMetadata.JobName.ToString())
                .Build();
        }

        async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            await Schedular.Shutdown();
        }
    }
}
