using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Models.User;
using ProtrndWebAPI.Settings;
using System.Security.Cryptography;
using System.Text;
using static MongoDB.Driver.WriteConcern;
using AccountDetailsDTO = ProtrndWebAPI.Models.Payments.AccountDetailsDTO;

namespace ProtrndWebAPI.Services
{
    public class PaymentService: BaseService
    {
        public PaymentService(IOptions<DBSettings> settings):base(settings)
        {
                
        }

        public async Task<AccountDetails?> AddAccountDetailsAsync(AccountDetailsDTO account, string token)
        {
            var accountDetails = new AccountDetails { AuthCode = account.AuthCode, CardNumber = EncryptDataWithAes(account.CardNumber, token), CVV = EncryptDataWithAes(account.CVV, token), ExpirtyDate = EncryptDataWithAes(account.ExpirtyDate, token), ProfileId = account.ProfileId };
            await _accountDetailsCollection.InsertOneAsync(accountDetails);
            var filter = Builders<Profile>.Filter.Eq(p => p.Identifier, account.ProfileId);
            var update = Builders<Profile>.Update.Set(s => s.AccountLinked, true);
            var updateResult = await _profileCollection.FindOneAndUpdateAsync(filter, update);
            if (accountDetails != null && updateResult != null)
                return accountDetails;
            return null;
        }

        private static string EncryptDataWithAes(string plainText, string token)
        {
            byte[] inputArray = Encoding.UTF8.GetBytes(plainText);
            var tripleDES = Aes.Create();
            tripleDES.Key = Encoding.UTF8.GetBytes(token);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        private string DecryptDataWithAes(byte[] cipherText, string token)
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

        public async Task<Transaction> GetTransactionByRefAsync(string reference)
        {
            return await _transactionCollection.Find(Builders<Transaction>.Filter.Eq(t => t.TrxRef, reference)).SingleOrDefaultAsync();
        }

        public async Task<bool> InsertTransactionAsync(Transaction transaction)
        {
            try
            {
                transaction.Id = Guid.NewGuid();
                transaction.Identifier = transaction.Id;
                await _transactionCollection.InsertOneAsync(transaction);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<int> GetTotalBalance(Guid profileId)
        {
            var filter = Builders<Transaction>.Filter.Where(s => s.ProfileId == profileId);
            var transactions = await _transactionCollection.Find(filter).ToListAsync();
            var total = 0;
            foreach (var transaction in transactions)
            {
                total += transaction.Amount;
            }
            return total;
        }

        public async Task<List<Promotion>> GetDuePromotions()
        {
            return await _promotionCollection.Find(p => !p.Disabled && p.NextCharge == DateTime.Today).ToListAsync();
        }

        public async Task<bool> UpdateNextPayDate(Promotion promotion)
        {
            var interval = promotion.ChargeIntervals;
            if (interval == "day")
                promotion.NextCharge.AddDays(1);
            if (interval == "week")
                promotion.NextCharge.AddWeeks(1);
            if (interval == "month")
                promotion.NextCharge.AddMonths(1);
            var filter = Builders<Promotion>.Filter.Where(p => !p.Disabled && p.ProfileId == promotion.ProfileId && p.Identifier == promotion.Identifier);
            var updateSuccess = await _promotionCollection.ReplaceOneAsync(filter, promotion);
            var transaction = new Transaction
            {
                Amount = promotion.Amount,
                ProfileId = promotion.Identifier,
                CreatedAt = DateTime.Now,
                TrxRef = Generate().ToString(),
                ItemId = promotion.PostId,
                Purpose = $"Pay for promotion id = {promotion.PostId}"
            };
            await InsertTransactionAsync(transaction);
            return updateSuccess.ModifiedCount > 0;
        }

        public async Task<bool> DisablePromotion(Promotion promotion)
        {
            var filter = Builders<Promotion>.Filter.Where(p => !p.Disabled && p.ProfileId == promotion.ProfileId && p.Identifier == promotion.Identifier);
            promotion.Disabled = true;
            var updateSuccess = await _promotionCollection.ReplaceOneAsync(filter, promotion);
            var transaction = new Transaction
            {
                Amount = promotion.Amount,
                ProfileId = promotion.Identifier,
                CreatedAt = DateTime.Now,
                TrxRef = Generate().ToString(),
                ItemId = promotion.PostId,
                Purpose = $"Payment failef for promotion id = {promotion.PostId}"
            };
            await InsertTransactionAsync(transaction);
            return updateSuccess.ModifiedCount > 0;
        }

        public async Task<bool> RequestWithdrawalAsync(Profile profile, int total)
        {
            // Modify withdrawal

            int balance = await GetTotalBalance(profile.Identifier);
            if (balance <= 100 || total < balance)
                return false;

            var transaction = new Transaction { Amount = -total, CreatedAt = DateTime.Now, ProfileId = profile.Identifier, Purpose = "Withdraw", TrxRef = Generate().ToString() };
            await _transactionCollection.InsertOneAsync(transaction);
            return true;
        }

        public async Task<int> GetTotalGiftsAsync(Guid profileId)
        {
            var gifts = await GetAllGiftAsync(profileId);
            return gifts.Count;
        }

        public async Task<List<Gift>> GetAllGiftAsync(Guid profileId)
        {
            var filter = Builders<Gift>.Filter.Where(s => s.ProfileId == profileId && s.Disabled == false);
            var gifts = await _giftsCollection.Find(filter).ToListAsync();
            return gifts;
        }

        public async Task<bool> BuyGiftsAsync(Guid profileId, int count)
        {
            var gifts = Enumerable.Repeat(new Gift { Id = null, ProfileId = profileId, Disabled = false }, count);
            try
            {
                await _giftsCollection.InsertManyAsync(gifts);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
