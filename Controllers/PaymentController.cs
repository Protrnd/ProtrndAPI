using Microsoft.AspNetCore.Mvc;
using MimeKit;
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
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _paymentService.GetFundsTotal(id) });
        }

        [HttpPost("set/{pin}")]
        public async Task<ActionResult<ActionResponse>> SetPaymentPin(string pin)
        {
            if (pin.Length < 4)
                return BadRequest(new ActionResponse { Data = "", Message = "Invalid Pin less that 4 digits", StatusCode = 400, Successful = false });
            var setPin = await _paymentService.SetPin(pin, _profileClaims.ID);
            return Ok(new ActionResponse { Data = setPin, Message = "Pin updated", StatusCode = 200, Successful = true });
        }

        [HttpGet("correct/{pin}")]
        public async Task<ActionResult<ActionResponse>> IsPinCorrect(string pin)
        {
            return Ok(new ActionResponse { Data = await _paymentService.IsPinCorrect(pin, _profileClaims.ID), Message = "Is Pin Correct Response", StatusCode = 200, Successful = true });
        }

        [HttpGet("get/pin")]
        public async Task<ActionResult<ActionResponse>> GetPaymentPin()
        {
            return Ok(new ActionResponse { Data = await _paymentService.PaymentPinExists(_profileClaims.ID), Message = "Pin Exists Response", StatusCode = 200, Successful = true });
        }

        [HttpPost("support/transfer")]
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
            await _paymentService.TopUpFunds(new Funds { Amount = dto.Amount * 0.9, ProfileId = dto.ReceiverId, Reference = dto.Reference, Time = DateTime.Now });

            var transaction = new Transaction
            {
                Amount = dto.Amount,
                ProfileId = _profileClaims.ID,
                ReceiverId = dto.ReceiverId,
                CreatedAt = DateTime.Now,
                TrxRef = Generate().ToString(),
                ItemId = support.Id,
                Purpose = $"Support sent to post with id = {dto.PostId}"
            };

            await _paymentService.InsertTransactionAsync(transaction);
            await _notificationService.SupportNotification(_profileClaims, dto.ReceiverId, dto.PostId, dto.Amount);
            return Ok(new ActionResponse
            {
                Successful = successful,
                Message = "OK",
                Data = "Support",
                StatusCode = 200
            });
        }

        [HttpPost("support/virtual")]
        public async Task<IActionResult> VirtualSupport(SupportDTO dto)
        {
            await Support(dto);
            var success = await _paymentService.TransferSupportFromBalance(_profileClaims.ID, dto.Amount);

            return Ok(new ActionResponse
            {
                Successful = success != "",
                Message = "OK",
                Data = "Support",
                StatusCode = 200
            });
        }

        [HttpPost("topup")]
        public async Task<IActionResult> TopUp(FundsDTO dto)
        {
            var funds = new Funds
            {
                ProfileId = _profileClaims.ID,
                Amount = dto.Amount,
                Time = DateTime.Now,
                Reference = dto.Reference
            };

            var successful = await _paymentService.TopUpFunds(funds);

            var transaction = new Transaction
            {
                Amount = dto.Amount,
                ProfileId = _profileClaims.ID,
                CreatedAt = DateTime.Now,
                TrxRef = funds.Reference,
                ItemId = _profileClaims.ID,
                Purpose = $"Topup ₦{dto.Amount}"
            };

            await _paymentService.InsertTransactionAsync(transaction);
            //await _notificationService.SupportNotification(_profileClaims, dto.ReceiverId, dto.PostId, dto.Amount);
            return Ok(new ActionResponse
            {
                Successful = successful,
                Message = "OK",
                Data = transaction.Id,
                StatusCode = 200
            });
        }

        [HttpPost("balance/to")]
        public async Task<IActionResult> SendFromBalance(FundsDTO dto)
        {
            var profile = await _profileService.GetProfileByIdAsync(dto.ProfileId);
            var transfer = await _paymentService.TransferFromBalance(_profileClaims.ID, dto.Amount, profile, dto.Reference);
            if (transfer == "")
                return BadRequest(new ActionResponse
                {
                    Successful = false,
                    Message = "Insufficient Funds",
                    Data = "Protrnd transfer",
                    StatusCode = 200
                });

            var funds = new Funds
            {
                ProfileId = dto.ProfileId,
                Amount = dto.Amount,
                Time = DateTime.Now,
                Reference = dto.Reference
            };

            var successful = await _paymentService.TopUpFunds(funds);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = dto.Amount,
                ProfileId = dto.ProfileId,
                CreatedAt = DateTime.Now,
                TrxRef = dto.Reference,
                ItemId = funds.Id,
                Purpose = $"Receive ₦{dto.Amount} from @{_profileClaims.UserName}"
            };

            await _paymentService.InsertTransactionAsync(transaction);

            return Ok(new ActionResponse
            {
                Successful = successful,
                Message = "OK",
                Data = transaction.Id,
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

        [HttpPost("funds/withdraw")]
        public async Task<IActionResult> WithdrawAllSupport(WithdrawDTO withdraw)
        {
            var withdrawalReference = await _paymentService.WithdrawFunds(_profileClaims.ID, withdraw.Amount);
            if (withdrawalReference != "")
            {
                SendWithdrawEmail(_profileClaims.Email, withdrawalReference, withdraw);
                ReceiveWithdrawEmail(withdrawalReference, _profileClaims.ID, withdraw);
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

        private string SendWithdrawEmail(string to, string reference, WithdrawDTO withdraw)
        {
            var from = _configuration[Constants.NoreplyEmailFrom];
            var connection = _configuration[Constants.NoreplyEmailConnection];
            var password = _configuration[Constants.NoreplyEmailPass];
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = $"Your withdrawal request to withdraw ₦{withdraw.Amount}";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = $"Your request for withdrawal reference is: {reference}. <p><b>Your account</b></p><p><b>Bank Name: {withdraw.Account.BankName}</b></p><p><b>Account Name: {withdraw.Account.AccountName}</b></p><p><b>Account Number: {withdraw.Account.AccountNumber}</b></p><p>Please be informed that you will receive <b>₦{withdraw.Amount - (withdraw.Amount * 0.05)}</b> and Protrnd will receive <b>₦{withdraw.Amount * 0.05}</b></p> <p>You will receive your payment within <b>72 hours</b> and if you do not receive it please send an email to us at protrndng@gmail.com. Thank you</p>" };
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

        private string ReceiveWithdrawEmail(string reference, Guid profileId, WithdrawDTO withdraw)
        {
            var from = _configuration[Constants.NoreplyEmailFrom];
            var connection = _configuration[Constants.NoreplyEmailConnection];
            var password = _configuration[Constants.NoreplyEmailPass];
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse("protrndng@gmail.com"));
            email.Cc.Add(MailboxAddress.Parse("jamesodike26@gmail.com"));
            email.Cc.Add(MailboxAddress.Parse("ifeanyiiiofurum@gmail.com "));
            email.Subject = $"Withdrawal request to withdraw ₦{withdraw.Amount}";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = $"Withdrawal request reference: <b>{reference}</b>. <p>Please note that the user will receive <b>₦{withdraw.Amount - (withdraw.Amount * 0.05)}</b></p> <p>Profile ID: <b>{profileId}</b></p><p><b>Withdrawal Account Details</b></p><p><b>Bank Name: {withdraw.Account.BankName}</b></p><p><b>Account Name: {withdraw.Account.AccountName}</b></p><p><b>Account Number: {withdraw.Account.AccountNumber}</b></p>" };
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
            return Ok(new ActionResponse { Successful = true, Message = $"Transactions results for page {page}", StatusCode = 200, Data = await _paymentService.GetTransactionsAsync(page, _profileClaims.ID, _profileClaims.UserName) });
        }

        [HttpGet("transaction/{id}")]
        public async Task<ActionResult<ActionResponse>> GetTransactionById(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, Message = $"Transaction data for {id}", StatusCode = 200, Data = await _paymentService.GetTransactionByIdAsync(id) });
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
