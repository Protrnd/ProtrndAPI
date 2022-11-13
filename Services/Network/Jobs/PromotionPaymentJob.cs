using Microsoft.Extensions.Options;
using PayStack.Net;
using ProtrndWebAPI.Services;
using ProtrndWebAPI.Settings;
using Quartz;

namespace CRONJOBTesting.Jobs
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

        public async Task Execute(IJobExecutionContext context)
        {
            var promotions = await PaymentService.GetDuePromotions();
            foreach (var promotion in promotions)
            {
                var response = PayStack.Charge.ChargeAuthorizationCode(new AuthorizationCodeChargeRequest { AuthorizationCode = promotion.AuthCode, Email = promotion.Email, Amount = (promotion.Amount * 100).ToString() });
                if (response.Data.Status == "success")
                    await PaymentService.UpdateNextPayDate(promotion);
                else
                    await PaymentService.DisablePromotion(promotion);
            }
        }
    }
}
