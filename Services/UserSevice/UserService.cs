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

        public Profile? GetProfile()
        {
            var result = new Profile();
            if (_contextAccessor != null && _contextAccessor.HttpContext != null)
            {
                try
                {
                    var user = _contextAccessor.HttpContext.User;
                    result.Email = user.FindFirstValue(Constants.Email);
                    result.Id = Guid.Parse(user.FindFirstValue(Constants.ID));
                    result.UserName = user.FindFirstValue(Constants.Name);
                    result.FullName = user.FindFirstValue(Constants.FullName);
                    result.AccountType = user.FindFirstValue(Constants.AccType);
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