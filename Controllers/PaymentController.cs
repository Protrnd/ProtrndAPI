using Microsoft.AspNetCore.Mvc;
using MimeKit;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Services.Network;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : BaseController
    {
        private readonly IConfiguration _configuration;

        public PaymentController(IServiceProvider serviceProvider, IConfiguration configuration) : base(serviceProvider)
        {
            _configuration = configuration;
        }

        [HttpGet("balance/{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetTotalBalance(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await _paymentService.GetFundsTotal(id) });
        }

        [HttpPost("set/{pin}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> SetPaymentPin(string pin)
        {
            if (pin.Length < 4)
                return BadRequest(new ActionResponse { Data = "", Message = "Invalid Pin less that 4 digits", StatusCode = 400, Successful = false });
            var setPin = await _paymentService.SetPin(pin, _profileClaims.ID);
            return Ok(new ActionResponse { Data = setPin, Message = "Pin updated", StatusCode = 200, Successful = true });
        }

        [HttpGet("correct/{pin}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> IsPinCorrect(string pin)
        {
            return Ok(new ActionResponse { Data = await _paymentService.IsPinCorrect(pin, _profileClaims.ID), Message = "Is Pin Correct Response", StatusCode = 200, Successful = true });
        }

        [HttpGet("get/pin")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetPaymentPin()
        {
            return Ok(new ActionResponse { Data = await _paymentService.PaymentPinExists(_profileClaims.ID), Message = "Pin Exists Response", StatusCode = 200, Successful = true });
        }

        [HttpPost("support/transfer")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> Support(SupportDTO dto)
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
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> VirtualSupport(SupportDTO dto)
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

        [HttpGet("withdrawal/all")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> GetAllWithdrawals()
        {
            return Ok(new ActionResponse { Data = await _paymentService.AdminGetAllWithdrawals(), Successful = true, Message = "All Withdrawals", StatusCode = 200 });
        }

        [HttpGet("funds/total")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> GetTotalFundss()
        {
            return Ok(new ActionResponse { Data = await _paymentService.AdminGetTotalFunds(), Successful = true, Message = "Total Funds", StatusCode = 200 });
        }

        [HttpPost("withdrawal/approve/{id}")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> ApproveWithdrawal(Guid id)
        {
            var action = await _paymentService.ApproveWithdrawal(id, _profileClaims.ID);
            return Ok(new ActionResponse { Data = action, Successful = action, Message = "Total Funds", StatusCode = 200 });
        }

        [HttpPost("withdrawal/reject/{id}")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> RejectWithdrawal(Guid id)
        {
            var action = await _paymentService.RejectWithdrawal(id, _profileClaims.ID);
            return Ok(new ActionResponse { Data = action, Successful = action, Message = "Rejected", StatusCode = 200 });
        }

        [HttpGet("revenue/total")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> GetRevenueTotal()
        {
            return Ok(new ActionResponse { Data = await _paymentService.GetTotalRevenue(), Successful = true, Message = "Total Revenue", StatusCode = 200 });
        }

        [HttpGet("revenue/range")]
        [ProTrndAuthorizationFilter(role: Constants.Admin)]
        public async Task<ActionResult<ActionResponse>> GetRevenueRange([FromQuery] FilterDate filter)
        {
            return Ok(new ActionResponse { Data = await _paymentService.RevenueSpan(filter.Start, filter.End), Successful = true, Message = "", StatusCode = 200 });
        }

        [HttpPost("topup")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> TopUp(FundsDTO dto)
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
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> SendFromBalance(FundsDTO dto)
        {
            var profile = await _profileService.GetProfileByIdAsync(dto.ProfileId);
            var transfer = await _paymentService.TransferFromBalance(dto.FromId, dto.Amount, profile, dto.Reference);
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
                ReceiverId = dto.FromId,
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
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetAllSupport()
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
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetAllSupportOnPost(Guid postId)
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
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> Withdraw(WithdrawDTO withdraw)
        {
            var withdrawalReference = await _paymentService.WithdrawFunds(_profileClaims.ID, withdraw.Amount, withdraw.Account);
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

        [HttpPost("forgot/pin")]
        public async Task<ActionResult<ActionResponse>> SendResetOtp()
        {
            var exists = await _paymentService.PaymentPinExists(_profileClaims.ID);
            if (exists)
            {
                var otp = SendResetEmail(_profileClaims.Email);
                return Ok(new ActionResponse
                {
                    Successful = exists,
                    Message = "Reset PIN OTP",
                    Data = otp,
                    StatusCode = 200
                });
            }
            return BadRequest(new ActionResponse
            {
                Successful = false,
                Message = "No Pin Set",
                Data = 0,
                StatusCode = 400
            });
        }

        private static int GenerateOTP()
        {
            var r = new Random();
            return r.Next(1000, 9999);
        }

        private int SendResetEmail(string to)
        {
            var from = _configuration[Constants.NoreplyEmailFrom];
            var connection = _configuration[Constants.NoreplyEmailConnection];
            var password = _configuration[Constants.NoreplyEmailPass];
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = "Your Protrnd One-Time-Password";
            var otp = GenerateOTP();
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = $"{getMailBodyTemplate(otp, "requested to reset you payment pin")}" };
            using var smtp = new SmtpClient();
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Connect(connection, 465);
            smtp.Authenticate(from, password);
            try
            {
                smtp.Send(email);
                return otp;
            }
            catch (Exception)
            {
                return 0;
            }
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

        private string getMailBodyTemplate(int otp, string type)
        {
            var body = "\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Protrnd</title>\r\n    <style>\r\n        *{\r\n            font-family:Arial, Helvetica, sans-serif;\r\n        }\r\n\r\n        body{\r\n            /* width: 100%; */\r\n            height: auto;\r\n\r\n            background-color: #d2d5e0;\r\n            width: 100%;\r\n            height: 100%;\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: center;\r\n            flex-direction: column;\r\n        }\r\n\r\n        /* .container{\r\n            background-color: #d2d5e0;\r\n            width: 100%;\r\n            height: 100%;\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: center;\r\n            flex-direction: column;\r\n\r\n        } */\r\n\r\n        .content{\r\n            background-color: white;\r\n            width: 450px;\r\n            margin-top: 20px;\r\n            margin-bottom: 20px;\r\n            border-radius: 0.5rem;\r\n            padding: 20px;\r\n        }\r\n\r\n        .discription{\r\n            line-height: 1.5rem;\r\n            font-size: 15px;\r\n            color: rgb(61, 59, 59);\r\n            \r\n        }\r\n\r\n        .nav{\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: space-between;\r\n        }\r\n\r\n        .nav > a{\r\n            text-decoration: none;\r\n            color: #423f3f;\r\n            font-weight: bold;\r\n            border: 2px solid #423f3f;\r\n            padding: 15px;\r\n            border-radius: 0.5rem;\r\n        }\r\n\r\n        .top-description{\r\n            font-weight: 100;\r\n            word-spacing: 0.2rem;\r\n            color: rgb(61, 59, 59);\r\n        }\r\n\r\n        .otp{\r\n            width: 100%;\r\n            background-color: #d2d5e0;\r\n            padding-top: 30px;\r\n            padding-bottom: 30px;\r\n            text-align: center;\r\n            font-weight: bold;\r\n            font-size: 50px;\r\n            border-radius: 0.5rem;\r\n            letter-spacing: 1rem;\r\n        }\r\n\r\n\r\n        .logo{\r\n            width: 40px;\r\n        }\r\n\r\n        .why{\r\n            width: 400px;\r\n            text-align: center;\r\n            font-size: 12px;\r\n            color: rgb(71, 68, 68);\r\n            font-weight: 600;\r\n            margin-bottom: 20px;\r\n        }\r\n\r\n    </style>\r\n</head>\r\n<body>\r\n    <!-- <p class=\"container\"> -->\r\n        <div class=\"content\">\r\n            <h1 class=\"heading\">\r\n                Complete registraion\r\n            </h1>\r\n            \r\n            <p class=\"discription top-description\">\r\n                To proceed, you need to complete this step before creating your Protrnd account. Please confirm this is right email address for your new account.\r\n                 Please enter this verification code to get started on Protrnd:\r\n            </p>\r\n    \r\n            \r\n            <p class=\"otp\">\r\n                {otpvalue}\r\n            </p>\r\n            \r\n            <p class=\"discription\">\r\n                If you did'nt create an account with Protrnd, please ignore this message. This OTP will be valid only for this request. Please do not close the otp page\r\n            </p>\r\n            <span class=\"discription\">\r\n                Thanks,\r\n            </span>\r\n                <br>\r\n            <span class=\"discription\">\r\n                Protrnd\r\n            </span>\r\n        </div>\r\n\r\n        <span class=\"why\">\r\n            You have received this email because you have {request} with Protrnd\r\n        </span>\r\n        \r\n    <!-- </p> -->\r\n</body>\r\n</html>";
            body.Replace("{request}", type);
            return body.Replace("{otpvalue}", otp.ToString());
        }

        [HttpGet("transactions/{page}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetTransactionsPaginated(int page)
        {
            return Ok(new ActionResponse { Successful = true, Message = $"Transactions results for page {page}", StatusCode = 200, Data = await _paymentService.GetTransactionsAsync(page, _profileClaims.ID, _profileClaims.UserName) });
        }

        [HttpGet("transaction/{id}")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> GetTransactionById(Guid id)
        {
            return Ok(new ActionResponse { Successful = true, Message = $"Transaction data for {id}", StatusCode = 200, Data = await _paymentService.GetTransactionByIdAsync(id) });
        }

        [HttpPost("verify/promotion")]
        [ProTrndAuthorizationFilter]
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
