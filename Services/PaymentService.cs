using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using PayStack.Net;
using ProtrndWebAPI.Models.Payments;
using ProtrndWebAPI.Settings;
using System.Security.Cryptography;
using System.Text;

namespace ProtrndWebAPI.Services
{
    public class PaymentService : BaseService
    {
        public PaymentService(IOptions<DBSettings> settings, IConfiguration configuration) : base(settings)
        {
        }

        //public async Task<AccountDetails?> AddAccountDetailsAsync(AccountDetailsDTO account)
        //{
        //    var accountExists = await GetAccountDetails(account.AccountNumber);
        //    AccountDetails? accountDetails;
        //    if (accountExists == null)
        //    {
        //        accountDetails = new AccountDetails { AccountNumber = account.AccountNumber, ProfileId = account.ProfileId };
        //        await _accountDetailsCollection.InsertOneAsync(accountDetails);
        //    }
        //    else
        //    {
        //        accountDetails = accountExists;
        //    }
        //    if (accountDetails != null)
        //        return accountDetails;
        //    return null;
        //}

        public async Task<List<Transaction>> GetTransactionsAsync(int page, Guid profileId, string username)
        {
            return await _transactionCollection.Find(Builders<Transaction>.Filter.Where(t => t.ProfileId == profileId && !t.Purpose.Contains(username) || t.ReceiverId == profileId && !t.Purpose.Contains(username)))
                .SortByDescending(t => t.CreatedAt)
                .Skip((page - 1) * 20)
                .Limit(20)
                .ToListAsync();
        }

        public async Task<Transaction> GetTransactionByIdAsync(Guid id)
        {
            return await _transactionCollection.Find(Builders<Transaction>.Filter.Eq(t => t.Id, id)).SingleOrDefaultAsync();
        }

        public async Task<bool> SupportAsync(Support support)
        {
            try
            {
                await _supportCollection.InsertOneAsync(support);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TopUpFunds(Funds funds)
        {
            try
            {
                await _fundsCollection.InsertOneAsync(funds);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<double> GetFundsTotal(Guid profileId)
        {
            var supports = await GetAllFunds(profileId);
            var total = 0d;
            foreach (var support in supports)
            {
                total += support.Amount;
            }
            return total;
        }

        public async Task<List<Support>> GetAllSupports(Guid profileId)
        {
            return await _supportCollection.Find(s => s.ReceiverId == profileId).ToListAsync();
        }

        public async Task<List<Funds>> GetAllFunds(Guid profileId)
        {
            return await _fundsCollection.Find(s => s.ProfileId == profileId).ToListAsync();
        }

        public async Task<List<Support>> GetSupportsOnPost(Guid postId)
        {
            return await _supportCollection.Find(s => s.PostId == postId).ToListAsync();
        }

        public async Task<string> WithdrawFunds(Guid profileId, int amount)
        {
            var supportTotal = await GetFundsTotal(profileId);
            if (supportTotal <= 0)
            {
                return "";
            }
            var funds = new Funds { Amount = -amount, ProfileId = profileId, Reference = GenerateReference().ToString(), Time = DateTime.Now };
            await _fundsCollection.InsertOneAsync(funds);
            await InsertTransactionAsync(new Transaction { Amount = -amount, CreatedAt= DateTime.Now, ItemId = funds.Id, ProfileId = profileId, Purpose = $"Withdraw ₦{amount}", ReceiverId = profileId, TrxRef = funds.Reference });
            return funds.Reference;
        }

        public async Task<string> TransferFromBalance(Guid profileId, double amount, Profile receiverProfile, string reference)
        {
            var fundsTotal = await GetFundsTotal(profileId);
            if (fundsTotal <= 0)
            {
                return "";
            }
            else
            {
                var funds = new Funds { Amount = -amount, ProfileId = profileId, Reference = reference };
                await _fundsCollection.InsertOneAsync(funds);
                await InsertTransactionAsync(new Transaction { Amount = -amount, CreatedAt = DateTime.Now, ItemId = funds.Id, ProfileId = profileId, Purpose = $"Transfer ₦{amount} to @{receiverProfile.UserName}", ReceiverId = receiverProfile.Id, TrxRef = funds.Reference });
                return funds.Reference;
            }
        }

        public async Task<string> TransferSupportFromBalance(Guid profileId, double amount)
        {
            var fundsTotal = await GetFundsTotal(profileId);
            if (fundsTotal <= 0)
            {
                return "";
            }
            var funds = new Funds { Amount = -amount, ProfileId = profileId, Reference = GenerateReference().ToString() };
            await _fundsCollection.InsertOneAsync(funds);
            return funds.Reference;
        }

        public async Task<int> GetSupportTotal(Guid profileId)
        {
            var supports = await GetAllSupports(profileId);
            var total = 0;
            foreach (var support in supports)
            {
                total += support.Amount;
            }
            return total;
        }

        private static int GenerateReference()
        {
            var r = new Random();
            return r.Next(1000, 9999);
        }

        public async Task<AccountDetails?> GetAccountDetails(string accountnumber)
        {
            var details = await _accountDetailsCollection.Find(a => a.AccountNumber == accountnumber).FirstOrDefaultAsync();
            return details;
        }

        public async Task<string> SetPin(string pin, Guid profileId)
        {
            var pinResult = await _pinCollection.Find(p => p.ProfileId == profileId).FirstOrDefaultAsync();
            CreateHash(pin, out byte[] pinHash, out byte[] pinSalt);
            if (pinResult != null)
            {
                pinResult.PaymentPinSalt = pinSalt;
                pinResult.PaymentPinHash = pinHash;
                var filter = Builders<PaymentPin>.Filter.Eq(p => p.ProfileId, profileId);
                await _pinCollection.ReplaceOneAsync(filter, pinResult);
            } 
            else
            {
                await _pinCollection.InsertOneAsync(new PaymentPin { PaymentPinHash = pinHash, PaymentPinSalt = pinSalt, ProfileId = profileId });
            }
            return pin;
        }

        public async Task<bool> IsPinCorrect(string pin, Guid profileId)
        {
            var pinResult = await _pinCollection.Find(p => p.ProfileId == profileId).FirstOrDefaultAsync();
            if (pinResult != null)
            {
                if (VerifyHash(pinResult.PaymentPinSalt, pin, pinResult.PaymentPinHash))
                    return true;
            }
            return false;
        }

        public async Task<bool> PaymentPinExists(Guid profileId)
        {
            var pinResult = await _pinCollection.Find(p => p.ProfileId == profileId).FirstOrDefaultAsync();
            return pinResult != null;
        }

        private static void CreateHash(string plaintext, out byte[] hash, out byte[] salt)
        {
            using var hmac = new HMACSHA512();
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plaintext));
        }

        private static bool VerifyHash(byte[] salt, string plaintext, byte[] hash)
        {
            using var hmac = new HMACSHA512(salt);
            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plaintext));
            return computeHash.SequenceEqual(hash);
        }
        //private static string EncryptDataWithAes(string plainText, string token)
        //{
        //    byte[] inputArray = Encoding.UTF8.GetBytes(plainText);
        //    var tripleDES = Aes.Create();
        //    tripleDES.Key = Encoding.UTF8.GetBytes(token);
        //    tripleDES.Mode = CipherMode.ECB;
        //    tripleDES.Padding = PaddingMode.PKCS7;
        //    ICryptoTransform cTransform = tripleDES.CreateEncryptor();
        //    byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
        //    tripleDES.Clear();
        //    return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        //}

        //private string DecryptDataWithAes(byte[] cipherText, string token)
        //{
        //    var tripleDES = Aes.Create();
        //    tripleDES.Key = Encoding.UTF8.GetBytes(token);
        //    tripleDES.Mode = CipherMode.ECB;
        //    tripleDES.Padding = PaddingMode.PKCS7;
        //    ICryptoTransform cTransform = tripleDES.CreateDecryptor();
        //    byte[] resultArray = cTransform.TransformFinalBlock(cipherText, 0, cipherText.Length);
        //    tripleDES.Clear();
        //    return Encoding.UTF8.GetString(resultArray);
        //}

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

        public async Task<bool> TransactionExistsAsync(string reference)
        {
            var exists = await _transactionCollection.Find(t => t.TrxRef == reference).SingleOrDefaultAsync();
            return exists != null;
        }


        public async Task<List<Promotion>> GetDuePromotions()
        {
            return await _promotionCollection.Find(p => !p.Disabled && p.ExpiryDate == DateTime.Now || p.ExpiryDate.AddMinutes(1) == DateTime.Now).ToListAsync();
        }

        public async Task<bool> UpdateNextPayDate(Promotion promotion)
        {
            var interval = promotion.ChargeIntervals;
            if (interval == "week")
                promotion.ExpiryDate.AddWeeks(1);
            if (interval == "month")
                promotion.ExpiryDate.AddMonths(1);
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
    }
}
