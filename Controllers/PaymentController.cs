using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver.Linq;
using PayStack.Net;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Services.Network;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/payment")]
    [ApiController]
    [ProTrndAuthorizationFilter]
    public class PaymentController : BaseController
    {
        private PayStackApi PayStack { get; set; }
        private readonly string accountToken;
        private readonly string token;

        public PaymentController(IServiceProvider serviceProvider, IConfiguration configuration) : base(serviceProvider)
        {
            token = configuration["Payment:PaystackSK"];
            accountToken = configuration["Payment:AccEncrypt"];
            PayStack = new(token);
        }

        [HttpPost("buy_gifts/balance/{count}")]
        public async Task<ActionResult<ActionResponse>> BuyGifts(int count)
        {
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            var trxRef = Generate().ToString();
            if (count < 1)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Cannot buy less than 1 gift" });
            var value = 500 * count;

            var totalBalance = await _paymentService.GetTotalBalance(_profile.Identifier);
            if (totalBalance < 0 && totalBalance <= value)
                return BadRequest(new ActionResponse { Message = "Error buying gifts" });

            var transaction = new Transaction
            {
                Amount = value,
                ProfileId = _profile.Identifier,
                CreatedAt = DateTime.Now,
                TrxRef = trxRef,
                ItemId = Guid.NewGuid(),
                Purpose = $"Purchase {count} gifts"
            };

            await _paymentService.InsertTransactionAsync(transaction);
            await _paymentService.BuyGiftsAsync(_profile.Identifier, count);
            return Ok(new ActionResponse { Successful = true, Data = trxRef, Message = ActionResponseMessage.Ok, StatusCode = 200 });
        }

        [HttpPost("balance")]
        public async Task<ActionResult<ActionResponse>> GetTotalBalance()
        {
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _paymentService.GetTotalBalance(_profile.Identifier) });
        }

        [HttpPost("top_up/balance/{total}")]
        public async Task<ActionResult<object>> TopUpBalance(int total)
        {
            return NotFound();
            TransactionInitializeRequest request = new()
            {
                AmountInKobo = total * 100,
                Email = _profile.Email,
                Reference = Generate().ToString(),
                Currency = "NGN"
                //CallbackUrl = ""
            };

            TransactionInitializeResponse response = PayStack.Transactions.Initialize(request);
            if (response.Status)
            {
                var transaction = new Transaction
                {
                    Amount = total,
                    ProfileId = _profile.Identifier,
                    CreatedAt = DateTime.Now,
                    TrxRef = request.Reference,
                    ItemId = Guid.NewGuid()
                };

                //await _paymentService.InsertTransactionAsync(transaction);
                return Ok(new { Success = true, Ref = request.Reference, Data = response.Data.AuthorizationUrl });
            }
            return BadRequest(new { Success = false, Ref = request.Reference, Data = "Error making transaction" });
        }

        [HttpPost("send_gifts/{id}/{count}")]
        public async Task<ActionResult<object>> SendGift(Guid id, int count)
        {
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            if (count < 1 || count > 100)
            {
                return BadRequest(new ActionResponse { Message = Constants.InvalidAmount });
            }
            var totalGifts = await _paymentService.GetTotalGiftsAsync(_profile.Identifier);
            if (totalGifts < count)
                return BadRequest(new ActionResponse { Message = "Insufficient Gifts" });
            var post = await _postsService.GetSinglePostAsync(id);
            if (post == null || !post.AcceptGift || post.ProfileId == _profile.Identifier)
                return BadRequest(new ActionResponse { Message = "Error accessing post" });

            var sent = await _postsService.SendGiftToPostAsync(post, count, _profile.Identifier);
            if (sent < 1)
                return BadRequest(new ActionResponse { Message = "Error sending gift" });

            var transaction = new Transaction
            {
                Amount = count,
                ProfileId = _profile.Identifier,
                CreatedAt = DateTime.Now,
                TrxRef = Generate().ToString(),
                ItemId = id,
                Purpose = $"Sending {count} gifts"
            };

            var responseOk = await _paymentService.InsertTransactionAsync(transaction);
            var notificationSent = await _notificationService.SendGiftNotification(_profile, post, count);
            if (responseOk && notificationSent)
                return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = $"{count} {count switch { > 1 => "gifts", < 2 => "gift" }} sent" });
            return BadRequest(new ActionResponse { Message = "Error sending gift" });
        }

        [HttpPost("withdraw/balance/{total}")]
        public async Task<IActionResult> RequestWithdrawal(int total)
        {
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            var success = await _paymentService.RequestWithdrawalAsync(_profile, total);
            if (success)
                return BadRequest(new ActionResponse { Successful = false, Message = "Error requesting withdrawal" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok });
        }

        [HttpPost("verify/promotion")]
        public async Task<ActionResult> VerifyPromotionPayment(VerifyPromotionTransaction promotionTransaction)
        {
            TransactionVerifyResponse response = PayStack.Transactions.Verify(promotionTransaction.Reference);
            if (response.Data.Status == "success")
            {
                if (_profile == null || _postsService == null || _paymentService == null)
                    return new ObjectResult(new ActionResponse
                    {
                        Successful = false,
                        Message = "Error making connection",
                        Data = response.Data.Reference,
                        StatusCode = 412
                    })
                    { StatusCode = 412 };
                var promotionDto = promotionTransaction.Promotion;
                var amount = response.Data.Amount / 100;
                var totalIsValid = amount == promotionDto.Amount;
                if (!totalIsValid)
                    return BadRequest(new ActionResponse
                    {
                        Successful = false,
                        Message = "Invalid Amount paid for promotion",
                        Data = response.Data.Reference,
                        StatusCode = 400
                    });
                var transaction = new Transaction
                {
                    Amount = amount,
                    ProfileId = _profile.Identifier,
                    CreatedAt = DateTime.Now,
                    TrxRef = response.Data.Reference,
                    ItemId = promotionDto.PostId,
                    Purpose = $"Pay for promotion id = {promotionDto.PostId}"
                };
                
                var verifyStatus = await _paymentService.InsertTransactionAsync(transaction);
                if (verifyStatus)
                {
                    promotionDto.AuthCode = response.Data.Authorization.AuthorizationCode;
                    promotionDto.Email = _profile.Email;
                    var promotion = new Promotion
                    {
                        CreatedAt = DateTime.Now,
                        Email = _profile.Email,
                        PostId = promotionDto.PostId,
                        Audience = promotionDto.Audience,
                        Amount = amount,
                        Currency = "NGN",
                        ChargeIntervals = promotionDto.ChargeIntervals,
                        AuthCode = response.Data.Authorization.AuthorizationCode,
                        BannerUrl = promotionDto.BannerUrl,
                        ProfileId = _profile.Identifier,
                        Categories = promotionDto.Categories
                    };
                    if (promotion.ChargeIntervals == "day")
                        promotion.NextCharge = DateTime.Now.AddDays(1);
                    if (promotion.ChargeIntervals == "week")
                        promotion.NextCharge = DateTime.Now.AddWeeks(1);
                    if (promotion.ChargeIntervals == "month")
                        promotion.NextCharge = DateTime.Now.AddMonths(1);
                    promotion.Identifier = promotion.Id;
                    var promotionOk = await _postsService.PromoteAsync(_profile, promotion);
                    if (promotionOk)
                        return Ok(new ActionResponse
                        {
                            Successful = true,
                            Message = response.Message,
                            Data = promotionOk,
                            StatusCode = 200
                        });
                }
            }
            return new ObjectResult(new ActionResponse
            {
                Successful = false,
                Message = response.Message,
                Data = null,
                StatusCode = 422
            })
            {
                StatusCode = 422
            };
        }

        [HttpPost("charge/promotion")]
        public async Task<ActionResult<ActionResponse>> ChargeCard(Promotion promotion)
        {
            var response = PayStack.Charge.ChargeAuthorizationCode(new AuthorizationCodeChargeRequest { AuthorizationCode = promotion.AuthCode, Email = _profile.Email, Amount = (promotion.Amount * 100).ToString() });
            if (response.Data.Status == "success")
                return Ok();
            return BadRequest();
        }

        [HttpPost("link/account")]
        public async Task<ActionResult<ActionResponse>> LinkAccount(AccountDetailsDTO accountDetailsDTO)
        {
            var accountLinked = await _paymentService.AddAccountDetailsAsync(accountDetailsDTO, accountToken);
            if (accountLinked == null)
                return BadRequest(new ActionResponse { Data = null, StatusCode = 400, Successful = false, Message = "Account Linking failed" });
            return Ok(new ActionResponse { Data = accountLinked, StatusCode = 200, Successful = true, Message = "Account Linked Successfully" });
        }

        [HttpPost("verify/accept_gift/{id}/{reference}")]
        public async Task<ActionResult<ActionResponse>> VerifyAcceptGift(Guid id, string reference)
        {
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            TransactionVerifyResponse response = PayStack.Transactions.Verify(reference);
            if (response.Data.Status == "success")
            {
                var transaction = new Transaction
                {
                    Amount = 1500,
                    ProfileId = _profile.Identifier,
                    CreatedAt = DateTime.Now,
                    TrxRef = response.Data.Reference,
                    ItemId = id,
                    //Status = true
                };

                var resultOk = await _paymentService.InsertTransactionAsync(transaction);
                if (resultOk)
                {
                    var acceptResultOk = await _postsService.AcceptGift(id);
                    if (acceptResultOk)
                        return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok });
                }
            }
            return BadRequest(new ActionResponse { Message = "Error verifying payment" });
        }

        [HttpPost("verify/top_up/{reference}")]
        public async Task<ActionResult> VerifyTopUpBalance(string reference)
        {
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            TransactionVerifyResponse response = PayStack.Transactions.Verify(reference);
            if (response.Data.Status == "success")
            {
                var amount = response.Data.Amount / 100;
                var transaction = new Transaction
                {
                    Amount = amount,
                    ProfileId = _profile.Identifier,
                    CreatedAt = DateTime.Now,
                    TrxRef = response.Data.Reference,
                    ItemId = Guid.NewGuid(),
                    Purpose = $"Top up {amount}"
                };
                var verifyStatus = await _paymentService.InsertTransactionAsync(transaction);
                if (verifyStatus)
                {
                    return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok });
                }
            }
            return BadRequest(new ActionResponse { Message = "Error verifying payment" });
        }

        //[HttpPost("verify/purchase/gift/{reference}")]
        //public async Task<ActionResult> VerifyGiftPurchase(string reference)
        //{
        //    var transaction = await _paymentService.GetTransactionByRefAsync(reference);
        //    if (transaction.ProfileId != _profile.Identifier)
        //        return Unauthorized(new DataResponse
        //        {
        //            Data = 403,
        //            Status = "Access dienied to the requested resource"
        //        });
        //    TransactionVerifyResponse response = PayStack.Transactions.Verify(reference);
        //    if (response.Data.Status == "success")
        //    {
        //        var count = response.Data.Amount / 50000;
        //        var verifyStatus = await _paymentService.VerifyTransactionAsync(transaction);
        //        if (verifyStatus != null && verifyStatus.Status)
        //        {
        //            var giftsBought = await _paymentService.BuyGiftsAsync(_profile.Identifier, count);
        //            if (giftsBought)
        //                return Ok(new BasicResponse { Success = true, Message = response.Message });
        //        }
        //    }
        //    return BadRequest(new BasicResponse { Message = "Error verifying payment" });
        //}

        private static int Generate()
        {
            Random r = new((int)DateTime.Now.Ticks);
            return r.Next(100000000, 999999999);
        }
    }
}
