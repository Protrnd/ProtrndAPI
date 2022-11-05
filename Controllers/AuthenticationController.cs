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
        
        [HttpGet]
        [ProTrndAuthorizationFilter]
        public ActionResult<ActionResponse> GetMe()
        {
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = _profile });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> Register(ProfileDTO request)
        {
            var userExists = await GetUserResult(request);

            if (userExists != null)
            {
                return BadRequest(new ActionResponse { Message = Constants.UserExists });
            }

            var otp = SendEmail(request.Email);
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = otp });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> ForgotPassword(string email)
        {
            var userExists = await GetUserResult(new ProfileDTO { Email = email });
            if (userExists == null)
            {
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            }
            //SendEmail(email)
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Email sent" });
        }

        [HttpPut("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> ResetPassword(ProfileDTO profile)
        {
            var register = await GetUserResult(new ProfileDTO { Email = profile.Email });
            if (register == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });

            CreatePasswordHash(profile.Password, out byte[] passwordHash, out byte[] passwordSalt);
            register.PasswordHash = passwordHash;
            register.PasswordSalt = passwordSalt;
            var result = await _regService.ResetPassword(register);
            if (result == null)
                return BadRequest(new ActionResponse { Message = "Error occurred when resetting password, please try again!" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok });
        }

        private static int SendEmail(string to)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("noreply@protrnd.com"));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = "Your ProTrnd One-Time-Password";
            var otp = GenerateOTP();
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = $"Your OTP is {otp}" };
            using var smtp = new SmtpClient();
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Connect("smtppro.zoho.com", 465);
            smtp.Authenticate("noreply@protrnd.com", "nrppt@$%JT22");
            smtp.Send(email);
            smtp.Disconnect(true);
            return otp;
        }

        [HttpPost("verify/otp/{type}")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> VerifyOTP(string type, ProfileDTO request)
        {
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            var register = new Register
            {
                Email = request.Email.Trim().ToLower(),
                PasswordSalt = passwordSalt,
                PasswordHash = passwordHash,
                UserName = request.UserName.Trim().ToLower(),
                FullName = request.FullName.Trim().ToLower(),
                RegistrationDate = DateTime.Now,
                AccountType = request.AccountType.Trim().ToLower(),
                Location = request.Location!.Trim().ToLower()
            };
            var result = await _regService.InsertAsync(register);
            if (result == null)
                return BadRequest(new ActionResponse { StatusCode = 400, Message = "Error registering user!" });
            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = ActionResponseMessage.Ok, Data = await LoginUser(register, type) }); ;
        }

        [HttpPost("login/{type}")]
        [AllowAnonymous]
        public async Task<ActionResult<ActionResponse>> Login(string type, [FromBody] Login login)
        {
            var result = await _regService.FindRegisteredUserByEmailAsync(login);

            if (result == null)
                return NotFound(new ActionResponse { StatusCode = 404, Message = ActionResponseMessage.NotFound });
            if (!VerifyPasswordHash(result, login.Password, result.PasswordHash))
                return BadRequest(new ActionResponse { StatusCode = 400, Message = Constants.WrongEmailPassword });

            return Ok(new ActionResponse { Successful = true, StatusCode = 200, Message = "Login Successful", Data = await LoginUser(result, type) });
        }

        [HttpPost("logout")]
        [ProTrndAuthorizationFilter]
        public ActionResult<ActionResponse> Logout()
        {
            try
            {
                // Modify logout function
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
                    new Claim(Constants.Name, user.UserName),
                    new Claim(Constants.Email, user.Email),
                    new Claim(Constants.FullName, user.FullName),
                    new Claim(Constants.AccType, user.AccountType),
                    new Claim(Constants.Location, user.Location),
                };

            bool disabled = false;
            if (user.AccountType == Constants.Disabled)
                disabled = true;

            claims.Add(new Claim(Constants.Disabled, disabled.ToString()));

            var sk = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration[Constants.TokenLoc]));
            var credentials = new SigningCredentials(sk, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddHours(6), signingCredentials: credentials);

            if (type == "cookie")
            {
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true, AllowRefresh = true, ExpiresUtc = DateTimeOffset.Now.AddMinutes(30) });
                return "";
            }
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }

        private static bool VerifyPasswordHash(Register user, string password, byte[] passwordHash)
        {
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computeHash.SequenceEqual(passwordHash);
        }

        private static int GenerateOTP()
        {
            var r = new Random();
            return r.Next(1000, 9999);
        }
    }
}
