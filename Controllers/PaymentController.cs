using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MongoDB.Driver.Linq;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Services.Network;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/payment")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class PaymentController : BaseController
    {
        private readonly IConfiguration _configuration;

        public PaymentController(IServiceProvider serviceProvider, IConfiguration configuration) : base(serviceProvider)
        {
            _configuration = configuration;
        }

        [HttpGet("balance/{id}")]
        public async Task<ActionResult<ActionResponse>> GetTotalBalance(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _paymentService.GetSupportTotal(id) });
        }

        [HttpPost("support")]
        public async Task<IActionResult> Support(SupportDTO dto)
        {
            var support = new Support
            {
                SenderId = _profileClaims.ID,
                ReceiverId = dto.ReceiverId,
                Amount = dto.Amount,
                Time = DateTime.Now,
                Reference = dto.Reference,
                PostId = dto.PostId
            };
            support.Identifier = support.Id;
            var successful = await _paymentService.SupportAsync(support);
            await _notificationService.SupportNotification(_profileClaims, dto.ReceiverId, dto.PostId, dto.Amount);
            return Ok(new ActionResponse
            {
                Successful = successful,
                Message = "OK",
                Data = "Support",
                StatusCode = 200
            });
        }

        [HttpGet("support/all")]
        public async Task<IActionResult> GetAllSupport()
        {
            var supports = await _paymentService.GetAllSupports(_profileClaims.ID);
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = supports,
                StatusCode = 200
            });
        }

        [HttpGet("support/all/post/{postId}")]
        public async Task<IActionResult> GetAllSupportOnPost(Guid postId)
        {
            var supports = await _paymentService.GetSupportsOnPost(postId);
            return Ok(new ActionResponse
            {
                Successful = true,
                Message = "OK",
                Data = supports,
                StatusCode = 200
            });
        }

        [HttpPost("support/withdraw")]
        public async Task<IActionResult> WithdrawAllSupport()
        {
            var withdrawalReference = await _paymentService.WithdrawSupports(_profileClaims.ID);
            if (withdrawalReference != "")
            {
                SendSupportWithdrawEmail(_profileClaims.Email, withdrawalReference);
                ReceiveSupportWithdrawEmail(withdrawalReference, _profileClaims.ID);
                return Ok(new ActionResponse
                {
                    Successful = true,
                    Message = "OK",
                    Data = withdrawalReference,
                    StatusCode = 200
                });
            }
            return BadRequest(new ActionResponse
            {
                Successful = false,
                Message = "Insufficient withdrawal funds",
                Data = withdrawalReference,
                StatusCode = 400
            });
        }

        private string SendSupportWithdrawEmail(string to, string reference)
        {
            var from = _configuration[Constants.NoreplyEmailFrom];
            var connection = _configuration[Constants.NoreplyEmailConnection];
            var password = _configuration[Constants.NoreplyEmailPass];
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = "Your withdrawal request";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = $"Your request for withdrawal reference is: {reference}. You will receive your payment within 72 hours and if you do not receive it please send an email to us at protrndng@gmail.com. Thank you" };
            using var smtp = new SmtpClient();
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Connect(connection, 465);
            smtp.Authenticate(from, password);
            try
            {
                smtp.Send(email);
                return reference;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private string ReceiveSupportWithdrawEmail(string reference, Guid profileId)
        {
            var from = _configuration[Constants.NoreplyEmailFrom];
            var connection = _configuration[Constants.NoreplyEmailConnection];
            var password = _configuration[Constants.NoreplyEmailPass];
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse("protrndng@gmail.com"));
            email.Cc.Add(MailboxAddress.Parse("jamesodike26@gmail.com"));
            email.Cc.Add(MailboxAddress.Parse("ifeanyiiiofurum@gmail.com "));
            email.Subject = "Withdrawal request";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = $"Withdrawal request reference: <b>{reference}</b>. <p>Profile ID: <b>{profileId}</b></p>" };
            using var smtp = new SmtpClient();
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Connect(connection, 465);
            smtp.Authenticate(from, password);
            try
            {
                smtp.Send(email);
                return reference;
            }
            catch (Exception)
            {
                return "";
            }
        }

        [HttpGet("transactions/{page}")]
        public async Task<ActionResult<ActionResponse>> GetTransactionsPaginated(int page)
        {
            return Ok(new ActionResponse { Successful = true, Message = $"Transactions results for page {page}", StatusCode = 200, Data = await _paymentService.GetTransactionsAsync(page, _profileClaims.ID) });
        }

        [HttpPost("verify/promotion")]
        public async Task<ActionResult<ActionResponse>> VerifyPromotionPayment(VerifyPromotionTransaction promotionTransaction)
        {
            if (_profileClaims == null || _postsService == null || _paymentService == null)
                return new ObjectResult(new ActionResponse
                {
                    Successful = false,
                    Message = "Error making connection",
                    Data = promotionTransaction.Reference,
                    StatusCode = 412
                })
                { StatusCode = 412 };
            var promotionDto = promotionTransaction.Promotion;
            var amount = promotionTransaction.Promotion.Amount;
            var transaction = new Transaction
            {
                Amount = amount,
                ProfileId = _profileClaims.ID,
                CreatedAt = DateTime.Now,
                TrxRef = promotionTransaction.Reference,
                ItemId = promotionDto.PostId,
                Purpose = $"Payment for promotion id = {promotionDto.PostId}"
            };

            var transactionExists = await _paymentService.TransactionExistsAsync(transaction.TrxRef);

            if (!transactionExists)
            {
                var verifyStatus = await _paymentService.InsertTransactionAsync(transaction);
                if (verifyStatus)
                {
                    DateTime expiry;
                    if (promotionDto.ChargeIntervals == "month")
                        expiry = DateTime.Now.AddDays(30);
                    else
                        expiry = DateTime.Now.AddDays(7);
                    var promotion = new Promotion
                    {
                        CreatedAt = DateTime.Now,
                        Email = promotionDto.Email,
                        PostId = promotionDto.PostId,
                        Audience = new Location { City = promotionDto.Audience.City, State = promotionDto.Audience.State },
                        Amount = amount,
                        ChargeIntervals = promotionDto.ChargeIntervals,
                        BannerUrl = promotionDto.BannerUrl,
                        ProfileId = promotionDto.ProfileId,
                        ExpiryDate = expiry,
                        Disabled = false,
                        Id = Guid.NewGuid()
                    };
                    promotion.Identifier = promotion.Id;
                    var promotionOk = await _postsService.PromoteAsync(promotion);
                    if (promotionOk)
                    {
                        await _notificationService.PromotionNotification(_profileClaims, promotion.Identifier);
                        return Ok(new ActionResponse
                        {
                            Successful = true,
                            Message = "OK",
                            Data = promotionOk,
                            StatusCode = 200
                        });
                    }
                }
            }

            return new ObjectResult(new ActionResponse
            {
                Successful = false,
                Message = "Error occured",
                Data = null,
                StatusCode = 422
            })
            {
                StatusCode = 422
            };
        }

        private static int Generate()
        {
            Random r = new((int)DateTime.Now.Ticks);
            return r.Next(100000000, 999999999);
        }
    }
}
