using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace ProtrndWebAPI.Services.UserSevice
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;
        }

        public TokenClaims? GetProfileTokenClaims()
        {
            var result = new TokenClaims();
            if (_contextAccessor != null && _contextAccessor.HttpContext != null)
            {
                var s = _contextAccessor.HttpContext.Request.Headers["Authorization"];
                if (AuthenticationHeaderValue.TryParse(s, out var headerValue))
                {
                    try
                    {
                        var scheme = headerValue.Scheme;
                        var parameter = headerValue.Parameter;
                        var stream = parameter;
                        var handler = new JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadToken(stream);
                        var tokenS = handler.ReadToken(stream) as JwtSecurityToken;
                        result.Email = tokenS.Claims.FirstOrDefault(a => a.Type == Constants.Email)?.Value;
                        result.ID = Guid.Parse(tokenS.Claims.FirstOrDefault(a => a.Type == Constants.ID)?.Value);
                        result.UserName = tokenS.Claims.FirstOrDefault(a => a.Type == Constants.UserName)?.Value;
                        result.Disabled = bool.Parse(tokenS.Claims.FirstOrDefault(a => a.Type == Constants.Disabled)?.Value);
                        result.Role = tokenS.Claims.FirstOrDefault(a => a.Type == Constants.Role)?.Value;
                        return result;
                    }
                    catch (Exception)
                    {
                        result = null;
                    }
                }
                else
                {
                    try
                    {
                        var user = _contextAccessor.HttpContext.User;
                        result.Email = user.FindFirstValue(Constants.Email);
                        result.ID = Guid.Parse(user.FindFirstValue(Constants.ID));
                        result.UserName = user.FindFirstValue(Constants.UserName);
                        result.Disabled = bool.Parse(user.FindFirstValue(Constants.Disabled));
                        result.Role = user.FindFirstValue(Constants.Role);
                        return result;
                    }
                    catch (Exception)
                    {
                        result = null;
                    }
                }
            }
            return result;
        }
    }
}