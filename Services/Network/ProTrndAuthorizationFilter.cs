using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using ProtrndWebAPI.Services.UserSevice;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace ProtrndWebAPI.Services.Network
{
    public class ProTrndAuthorizationFilter : Attribute, IAuthorizationFilter
    {
        readonly string[] _requiredClaims;
        string _role;

        public ProTrndAuthorizationFilter(string role = "", params string[] claims)
        {
            _role = role;
            _requiredClaims = claims;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var s = context.HttpContext.Request.Headers["Authorization"];
            if (AuthenticationHeaderValue.TryParse(s, out var headerValue))
            {
                var scheme = headerValue.Scheme;
                var parameter = headerValue.Parameter;
                var stream = parameter;
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(stream);
                var tokenS = handler.ReadToken(stream) as JwtSecurityToken;
                var issuer = tokenS.Claims.FirstOrDefault(a => a.Type == "iss")?.Value;
                if (issuer != null && issuer == "protrnd.com") 
                {
                    if (_role == Constants.Admin)
                    {
                        var role = tokenS.Claims.FirstOrDefault(a => a.Type == Constants.Role)?.Value;
                        if (role != null && role != Constants.Admin)
                        {
                            context.Result = new ObjectResult(new ActionResponse { StatusCode = 403, Message = "User is forbidden, Route only for admins" }) { StatusCode = 403 };
                            return;
                        }
                    }
                    else
                    {
                        var disabled = tokenS.Claims.FirstOrDefault(r => r.Type == Constants.Disabled)?.Value;
                        if (disabled == null || disabled != Constants.False)
                        {
                            context.Result = new ObjectResult(new ActionResponse { StatusCode = 403, Message = "User account disabled" }) { StatusCode = 403 };
                            return;
                        }
                    }
                }
                else
                {
                    context.Result = new ObjectResult(new ActionResponse { StatusCode = 403, Message = "Invalid claims" }) { StatusCode = 403 };
                    return;
                }
            }
            else
            {
                var profileService = context.HttpContext.RequestServices.GetService(typeof(IUserService));
                if (profileService is not IUserService isAuthenticated || isAuthenticated.GetProfileTokenClaims() == null)
                {
                    context.Result = new UnauthorizedObjectResult(new ActionResponse { StatusCode = 401, Message = "User is unauthorized" });
                    return;
                }

            }
        }
    }
}
