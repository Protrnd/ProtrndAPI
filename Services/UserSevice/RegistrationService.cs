using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProtrndWebAPI.Settings;

namespace ProtrndWebAPI.Services.UserSevice
{
    public class RegistrationService : BaseService
    {
        public RegistrationService(IOptions<DBSettings> settings) : base(settings) { }

        public async Task<Profile> InsertAsync(Register register)
        {
            await _registrationCollection.InsertOneAsync(register);
            var userProfile = new Profile
            {
                Id = register.Id,
                Identifier = register.Id,
                UserName = register.UserName.ToLower(),
                Email = register.Email.ToLower(),
                AccountType = register.AccountType,
                RegistrationDate = register.RegistrationDate,
                Disabled = false,
                FullName = register.FullName
            };
            await _profileCollection.InsertOneAsync(userProfile);
            return userProfile;
        }

        public async Task<Register> ResetPassword(Register register)
        {
            var filter = Builders<Register>.Filter.Eq(r => r.Email, register.Email.Trim().ToLower());
            await _registrationCollection.ReplaceOneAsync(filter, register);
            return register;
        }

        public async Task<Register?> FindRegisteredUserAsync(ProfileDTO register)
        {
            return await _registrationCollection.Find(r => r.Email == register.Email.Trim().ToLower() || r.UserName == register.UserName.Trim().ToLower() && r.AccountType != Constants.Disabled).FirstOrDefaultAsync();            
        }

        public async Task<Register?> FindRegisteredUserByEmailAsync(Login login)
        {
            return await _registrationCollection.Find(r => r.Email == login.Email.Trim().ToLower() && r.AccountType != Constants.Disabled).FirstOrDefaultAsync();
        }
    }
}
