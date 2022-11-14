using Microsoft.Extensions.Options;
using PayStack.Net;
using ProtrndWebAPI.Services;
using ProtrndWebAPI.Settings;
using Quartz;

namespace ProtrndWebAPI .Jobs
{
    public class PromotionPaymentJob : IJob
    {
        private PayStackApi PayStack { get; set; }
        PaymentService PaymentService { get; set; }
        public PromotionPaymentJob(IConfiguration configuration, IOptions<DBSettings> settings)
        {
            PayStack = new(configuration["Payment:PaystackSK"]);
            PaymentService = new PaymentService(settings);
        }

        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Hello World");
            //Task.Run(async () =>
            //{
            //    var promotions = await PaymentService.GetDuePromotions();
            //    Console.WriteLine("Promotions checked");
            //    foreach (var promotion in promotions)
            //    {
            //        var response = PayStack.Charge.ChargeAuthorizationCode(new AuthorizationCodeChargeRequest { AuthorizationCode = promotion.AuthCode, Email = promotion.Email, Amount = (promotion.Amount * 100).ToString() });
            //        if (response.Data.Status == "success")
            //        {
            //            await PaymentService.UpdateNextPayDate(promotion);
            //            Console.WriteLine("Charged and updated");
            //        }
            //        else
            //            await PaymentService.DisablePromotion(promotion);
            //    }
            //});
            
            return Task.CompletedTask;
        }
    }
}
