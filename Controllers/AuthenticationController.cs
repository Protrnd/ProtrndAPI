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
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = getMailBodyTemplate(otp) };
            using var smtp = new SmtpClient();
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Connect(connection, 465);
            smtp.Authenticate(from, password);
            smtp.Send(email);
            smtp.Disconnect(true);
            return otp;
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
            //var root = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).Replace("\\bin\\Debug\\net6.0", "");
            //using StreamReader reader = new(root + @"\StaticFiles\emailtemplate.html");
            string? body = $"Your otp is {otp}";
            body = body.Replace("{otpvalue}", otp.ToString());
            return body;
        }
    }
}
