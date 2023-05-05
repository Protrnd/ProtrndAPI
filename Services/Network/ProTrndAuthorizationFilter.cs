using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

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
            if (context.HttpContext.RequestServices.GetService(typeof(IUserService)) is not IUserService isAuthenticated || isAuthenticated.GetProfileTokenClaims() == null)
            {
                context.Result = new UnauthorizedObjectResult(new ActionResponse { StatusCode = 401, Message = "User is unauthorized" });
                return;
            }

            var hasAllRequiredClaims = _requiredClaims.All(claim => context.HttpContext.User.HasClaim(x => x.Type == claim));
            var claimsIdentity = context.HttpContext.User.Identity as ClaimsIdentity;
            var isAdmin = claimsIdentity.HasClaim(Constants.Role, Constants.Admin);
            if (!hasAllRequiredClaims)
            {
                context.Result = new ObjectResult(new ActionResponse { StatusCode = 403, Message = "User is forbidden, Invalid claims" }) { StatusCode = 403 };
                return;
            }
            if (_role == Constants.Admin && !isAdmin)
            {
                context.Result = new ObjectResult(new ActionResponse { StatusCode = 403, Message = "User is forbidden, Route only for admins" }) { StatusCode = 403 };
                return;
            }
        }
    }
}
