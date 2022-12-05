using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using ProtrndWebAPI.Services.Network;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Constants = ProtrndWebAPI.Models.Constants;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace ProtrndWebAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : BaseController
    {
        private readonly IConfiguration _configuration;

        public AuthenticationController(IConfiguration configuartion, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _configuration = configuartion;
        }

        private static byte[] EncryptDataWithAes(string plainText, string token)
        {
            byte[] inputArray = Encoding.UTF8.GetBytes(plainText);
            var tripleDES = Aes.Create();
            tripleDES.Key = Encoding.UTF8.GetBytes(token);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return resultArray;
        }

        private static string DecryptDataWithAes(byte[] cipherText, string token)
        {
            var tripleDES = Aes.Create();
            tripleDES.Key = Encoding.UTF8.GetBytes(token);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(cipherText, 0, cipherText.Length);
            tripleDES.Clear();
            return Encoding.UTF8.GetString(resultArray);
        }

        [HttpGet]
        [ProTrndAuthorizationFilter]
        public ActionResult<ActionResponse> GetMe()
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = _profileClaims });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> Register(RegisterDTO request)
        {
            var userExists = await GetUserResult(new ProfileDTO
            {
                Email = request.Email,
                FullName = request.FullName,
                UserName = request.UserName
            });

            if (userExists != null)
            {
                return BadRequest(new ActionResponse { Message = Constants.UserExists });
            }

            var otp = SendOtpEmail(request.Email);
            return Ok(new ActionResponse
            {
                Successful = true,
                StatusCode = 200,
                Message = ActionResponseMessage.Ok,
                Data = EncryptDataWithAes(otp.ToString(), _configuration[Constants.OTPEncryptionRoute])
            });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> ForgotPassword([FromBody]Login login)
        {
            var userExists = await GetUserResult(new ProfileDTO { Email = login.Email });
            if (userExists == null)
            {
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound, Successful = false, Data = null });
            }

            var otp = SendOtpEmail(login.Email);
            return Ok(new ActionResponse
            {
                Successful = true,
                StatusCode = 200,
                Message = "Email sent",
                Data = EncryptDataWithAes(otp.ToString(), _configuration[Constants.OTPEncryptionRoute])
            });
        }

        [HttpPut("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> ResetPassword(ResetPasswordDTO resetPasswordDto)
        {
            var register = await GetUserResult(new ProfileDTO { Email = resetPasswordDto.Reset.Email });
            if (_regService == null || register == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });

            CreateHash(resetPasswordDto.Reset.Password, out byte[] passwordHash, out byte[] passwordSalt);
            register.PasswordHash = passwordHash;
            register.PasswordSalt = passwordSalt;
            var result = await _regService.ResetPassword(register);
            if (result == null)
                return BadRequest(new ActionResponse { StatusCode = 400, Data = null, Successful = false, Message = "Error occurred when resetting password, please try again!" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok });
        }

        private int SendOtpEmail(string to)
        {
            var from = _configuration[Constants.NoreplyEmailFrom];
            var connection = _configuration[Constants.NoreplyEmailConnection];
            var password = _configuration[Constants.NoreplyEmailPass];
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = "Your ProTrnd One-Time-Password";
            var otp = GenerateOTP();
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = $"{getMailBodyTemplate(otp)}" };
            using var smtp = new SmtpClient();
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Connect(connection, 465);
            smtp.Authenticate(from, password);
            try
            {
                smtp.Send(email);
                smtp.Disconnect(true);
            }
            catch (Exception e)
            {
                return otp;
            }
            return 0;
        }

        [HttpPost("verify/otp")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> VerifyOTP(VerifyOTPSalt verify)
        {
            var otp = DecryptDataWithAes(verify.OTPHash, _configuration[Constants.OTPEncryptionRoute]);
            if (_regService == null || otp != verify.PlainText)
                return new ObjectResult(new ActionResponse { StatusCode = 403, Message = "Invalid otp inserted", Successful = false, Data = false }) { StatusCode = 403 };
            var request = verify.RegisterDto;
            CreateHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            var register = new Register
            {
                Email = request.Email.Trim().ToLower(),
                PasswordSalt = passwordSalt,
                PasswordHash = passwordHash,
                UserName = request.UserName.Trim().ToLower(),
                FullName = request.FullName.Trim().ToLower(),
                RegistrationDate = DateTime.Now,
                AccountType = request.AccountType.Trim().ToLower()
            };
            var result = await _regService.InsertAsync(register);
            if (result == null)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Error registering user!" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await LoginUser(register, verify.Type) }); ;
        }

        [HttpPost("login/{type}")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> Login(string type, [FromBody] Login login)
        {
            var result = await _regService.FindRegisteredUserByEmailAsync(login);

            if (result == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            if (!VerifyHash(result.PasswordSalt, login.Password, result.PasswordHash))
                return BadRequest(new ActionResponse { StatusCode = 400, Message = Constants.WrongEmailPassword });

            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Login Successful", Data = await LoginUser(result, type) });
        }

        [HttpPost("logout")]
        [ProTrndAuthorizationFilter]
        public async Task<ActionResult<ActionResponse>> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync();
                return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok });
            }
            catch (Exception)
            {
                return BadRequest(new { Success = false, Message = "Logout failed" });
            }
        }

        private async Task<Register?> GetUserResult(ProfileDTO request)
        {
            return await _regService.FindRegisteredUserAsync(request);
        }

        private async Task<string?> LoginUser(Register user, string type)
        {
            List<Claim> claims = new()
                {
                    new Claim(Constants.ID, user.Id.ToString()),
                    new Claim(Constants.UserName, user.UserName.ToString()),
                    new Claim(Constants.Email, user.Email.ToString()),
                    new Claim(Constants.Disabled, (user.AccountType == Constants.Disabled).ToString())
                };

            if (type == "cookie")
            {
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true, AllowRefresh = true, ExpiresUtc = DateTimeOffset.Now.AddYears(2) });
                return "";
            }
            else if (type == "jwt")
            {
                var sk = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration[Constants.TokenLoc]));
                var credentials = new SigningCredentials(sk, SecurityAlgorithms.HmacSha512Signature);
                var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddYears(2), signingCredentials: credentials, issuer: "protrnd.com", audience: "https://protrnd.com");
                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                return jwt;
            }
            return null;
        }

        private static void CreateHash(string plaintext, out byte[] hash, out byte[] salt)
        {
            using var hmac = new HMACSHA512();
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plaintext));
        }

        private static bool VerifyHash(byte[] salt, string plaintext, byte[] hash)
        {
            using var hmac = new HMACSHA512(salt);
            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plaintext));
            return computeHash.SequenceEqual(hash);
        }

        private static int GenerateOTP()
        {
            var r = new Random();
            return r.Next(1000, 9999);
        }

        private string getMailBodyTemplate(int otp)
        {
            var body = "\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Protrnd</title>\r\n    <style>\r\n        *{\r\n            font-family:Arial, Helvetica, sans-serif;\r\n        }\r\n\r\n        body{\r\n            /* width: 100%; */\r\n            height: auto;\r\n\r\n            background-color: #d2d5e0;\r\n            width: 100%;\r\n            height: 100%;\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: center;\r\n            flex-direction: column;\r\n        }\r\n\r\n        /* .container{\r\n            background-color: #d2d5e0;\r\n            width: 100%;\r\n            height: 100%;\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: center;\r\n            flex-direction: column;\r\n\r\n        } */\r\n\r\n        .content{\r\n            background-color: white;\r\n            width: 450px;\r\n            margin-top: 20px;\r\n            margin-bottom: 20px;\r\n            border-radius: 0.5rem;\r\n            padding: 20px;\r\n        }\r\n\r\n        .discription{\r\n            line-height: 1.5rem;\r\n            font-size: 15px;\r\n            color: rgb(61, 59, 59);\r\n            \r\n        }\r\n\r\n        .nav{\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: space-between;\r\n        }\r\n\r\n        .nav > a{\r\n            text-decoration: none;\r\n            color: #423f3f;\r\n            font-weight: bold;\r\n            border: 2px solid #423f3f;\r\n            padding: 15px;\r\n            border-radius: 0.5rem;\r\n        }\r\n\r\n        .top-description{\r\n            font-weight: 100;\r\n            word-spacing: 0.2rem;\r\n            color: rgb(61, 59, 59);\r\n        }\r\n\r\n        .otp{\r\n            width: 100%;\r\n            background-color: #d2d5e0;\r\n            padding-top: 30px;\r\n            padding-bottom: 30px;\r\n            text-align: center;\r\n            font-weight: bold;\r\n            font-size: 50px;\r\n            border-radius: 0.5rem;\r\n            letter-spacing: 1rem;\r\n        }\r\n\r\n\r\n        .logo{\r\n            width: 40px;\r\n        }\r\n\r\n        .why{\r\n            width: 400px;\r\n            text-align: center;\r\n            font-size: 12px;\r\n            color: rgb(71, 68, 68);\r\n            font-weight: 600;\r\n            margin-bottom: 20px;\r\n        }\r\n\r\n    </style>\r\n</head>\r\n<body>\r\n    <!-- <p class=\"container\"> -->\r\n        <div class=\"content\">\r\n            <h1 class=\"heading\">\r\n                Complete registraion\r\n            </h1>\r\n            \r\n            <p class=\"discription top-description\">\r\n                To proceed, you need to complete this step before creating your Protrnd account. Please confirm this is right email address for your new account.\r\n                 Please enter this verification code to get started on Protrnd:\r\n            </p>\r\n    \r\n            \r\n            <p class=\"otp\">\r\n                {otpvalue}\r\n            </p>\r\n            \r\n            <p class=\"discription\">\r\n                If you did'nt create an account with Protrend, please ignore this message. This OTP will be valid only for this request. Please do not close the otp page\r\n            </p>\r\n            <span class=\"discription\">\r\n                Thanks,\r\n            </span>\r\n                <br>\r\n            <span class=\"discription\">\r\n                Protrnd\r\n            </span>\r\n        </div>\r\n\r\n        <span class=\"why\">\r\n            You have received this email because you have signed up for an account with Protrnd\r\n        </span>\r\n        \r\n    <!-- </p> -->\r\n</body>\r\n</html>";
            return body.Replace("{otpvalue}", otp.ToString());
        }
    }
}
