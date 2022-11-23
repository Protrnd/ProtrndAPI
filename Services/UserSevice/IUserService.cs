namespace ProtrndWebAPI.Services.UserSevice
{
    public interface IUserService
    {
        TokenClaims? GetProfileTokenClaims();
    }
}
