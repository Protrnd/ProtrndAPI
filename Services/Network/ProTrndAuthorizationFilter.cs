using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProtrndWebAPI.Services.Network
{
    public class ProTrndAuthorizationFilter : Attribute, IAuthorizationFilter
    {
        readonly string[] _requiredClaims;

        public ProTrndAuthorizationFilter(params string[] claims)
        {
            _requiredClaims = claims;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.HttpContext.RequestServices.GetService(typeof(IUserService)) is not IUserService isAuthenticated || isAuthenticated.GetProfile() == null)
            {
                context.Result = new UnauthorizedObjectResult(new ActionResponse { StatusCode = 401, Message = "User is unauthorized" });
                return;
            }

            var hasAllRequiredClaims = _requiredClaims.All(claim => context.HttpContext.User.HasClaim(x => x.Type == claim));
            if (!hasAllRequiredClaims)
            {
                context.Result = new ObjectResult(new ActionResponse { StatusCode = 403, Message = "User is forbidden, Invalid claims" }) { StatusCode = 403 };
                return;
            }
        }
    }
}
