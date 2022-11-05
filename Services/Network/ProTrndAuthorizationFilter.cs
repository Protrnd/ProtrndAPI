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
            var isAuthenticated = context.HttpContext.RequestServices.GetService(typeof(IUserService)) as IUserService;

            if (isAuthenticated.GetProfile() == null)
            {
                context.Result = new UnauthorizedObjectResult(new ActionResponse { StatusCode = 401, Message = "User is unauthorized" });
                return;
            }

            var hasAllRequiredClaims = _requiredClaims.All(claim => context.HttpContext.User.HasClaim(x => x.Type == claim));
            if (!hasAllRequiredClaims)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
