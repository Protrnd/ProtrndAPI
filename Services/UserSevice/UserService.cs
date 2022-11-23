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
                try
                {
                    var user = _contextAccessor.HttpContext.User;
                    result.Email = user.FindFirstValue(Constants.Email);
                    result.ID = Guid.Parse(user.FindFirstValue(Constants.ID));
                    result.UserName = user.FindFirstValue(Constants.Name);
                    result.Location = user.FindFirstValue(Constants.Location);
                    result.Disabled = bool.Parse(user.FindFirstValue(Constants.Disabled));
                    return result;
                }
                catch (Exception)
                {
                    result = null;
                }
            }
            return result;
        }
    }
}