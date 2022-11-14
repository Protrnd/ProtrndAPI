using Quartz;
using Quartz.Spi;

namespace ProtrndWebAPI.JobFactory
{
    public class JobFactory : IJobFactory
    {
        private IServiceProvider serviceProvider;
        public JobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        IJob IJobFactory.NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobDetail = bundle.JobDetail;
            return (IJob)serviceProvider.GetService(jobDetail.JobType);
        }

        void IJobFactory.ReturnJob(IJob job)
        {
            return;
        }
    }
}
