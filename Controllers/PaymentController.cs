using Microsoft.AspNetCore.Mvc;
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

        private readonly string token;

        public PaymentController(IServiceProvider serviceProvider, IConfiguration configuration) : base(serviceProvider)
        {
            token = configuration["Payment:PaystackSK"];
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
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
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

                await _paymentService.InsertTransactionAsync(transaction);
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
        public async Task<ActionResult> VerifyPromotionPayment(VerifyTransaction promotion)
        {
            return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            TransactionVerifyResponse response = PayStack.Transactions.Verify(promotion.Reference);
            if (response.Data.Status == "success")
            {
                var promotionDto = promotion.Type as Promotion;
                var transaction = new Transaction
                {
                    Amount = 1500,
                    ProfileId = _profile.Identifier,
                    CreatedAt = DateTime.Now,
                    TrxRef = response.Data.Reference,
                    ItemId = promotionDto.PostId,
                    Purpose = $"Pay for promotion id = {promotionDto.PostId}"
                };

                var verifyStatus = await _paymentService.InsertTransactionAsync(transaction);
                if (verifyStatus)
                {
                    var promotionOk = await _postsService.PromoteAsync(_profile, promotionDto);
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
            return BadRequest(new ActionResponse
            {
                Successful = false,
                Message = response.Message,
                Data = null,
                StatusCode = 422
            });
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
