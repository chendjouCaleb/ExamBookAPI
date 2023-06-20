using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExamBook.Identity.Services
{
    public class UserCodeService
    {
        private readonly ApplicationIdentityDbContext _dbContext;
        private readonly ILogger<UserCodeService> _logger;

        public UserCodeService(ApplicationIdentityDbContext dbContext, ILogger<UserCodeService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserCode> GetAsync(string userId, string purpose)
        {
            var normalizedUserId = StringHelper.Normalize(userId);
            var query = _dbContext.Set<UserCode>()
                .Where(uc => uc.NormalizedUserId == normalizedUserId);

            if (!await query.AnyAsync())
            {
                throw new ElementNotFoundException("UserCodeNotFoundByUserId{0}", userId);
            }

            var userCode = await query.Where(uc => uc.Purpose == purpose)
                .FirstOrDefaultAsync();

            if (userCode is null)
            {
                throw new ElementNotFoundException("UserCodeNotFoundByPurpose{0}{1}", userId, purpose);
            }

            return userCode;
        }
        
        
        public async Task<UserCode?> GetOrNullAsync(string userId, string purpose)
        {
            var normalizedUserId = StringHelper.Normalize(userId);
            return await _dbContext.Set<UserCode>()
                .Where(uc => uc.NormalizedUserId == normalizedUserId)
                .Where(uc => uc.Purpose == purpose)
                .FirstOrDefaultAsync();
        }
        
        
        
        public void CheckCode(UserCode userCode, string code)
        {
            AssertHelper.NotNull(userCode, nameof(userCode));

            var isValid = BCrypt.Net.BCrypt.EnhancedVerify(code, userCode.CodeHash);
            if (!isValid)
            {
                throw new IllegalValueException("InvalidUserCode");
            }
        }
        
        public async Task<bool> IsValidCode(string userId, string purpose, string code)
        {
            var userCode = await GetAsync(userId, purpose);

            return BCrypt.Net.BCrypt.EnhancedVerify(code, userCode.CodeHash);
        }


        public async Task<UserCodeCreateResult> AddOrUpdateCodeAsync(string userId, string purpose)
        {
            AssertHelper.NotNullOrWhiteSpace(userId, nameof(userId));
            AssertHelper.NotNullOrWhiteSpace(purpose, nameof(purpose));
            
            var userCode = await GetOrNullAsync(userId, purpose);

            if (userCode != null)
            {
                var code = _GenerateCode();
                userCode.CodeHash = HashCode(code);
                userCode.UpdateDate = DateTime.UtcNow;
                
                _dbContext.Update(userCode);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine("Code: " + code);
                return new UserCodeCreateResult {Code = code, UserCode = userCode};
            }


            return await AddCodeAsync(userId, purpose);
        }


        public async Task<UserCodeCreateResult> AddCodeAsync(string userId, string purpose)
        {
            AssertHelper.NotNullOrWhiteSpace(userId, nameof(userId));
            AssertHelper.NotNullOrWhiteSpace(purpose, nameof(purpose));

            var code = _GenerateCode();
            var codeHash = HashCode(code);
            UserCode userCode = new ()
            {
                UserId = userId,
                NormalizedUserId = StringHelper.Normalize(userId),
                CodeHash = codeHash,
                Purpose = purpose,
                UpdateDate = DateTime.UtcNow
            };
            await _dbContext.AddAsync(userCode);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New code generated");
            return new UserCodeCreateResult { UserCode = userCode, Code = code };
        }


        public async Task DeleteAsync(UserCode userCode)
        {
            AssertHelper.NotNull(userCode, nameof(userCode));

            _dbContext.Remove(userCode);
            await _dbContext.SaveChangesAsync();
        }


        private string _GenerateCode()
        {
            return StringHelper.Normalize(Guid.NewGuid().ToString()[..6]);
        }

        private string HashCode(string code)
        {
            return BCrypt.Net.BCrypt.EnhancedHashPassword(code, 4);
        }
    }
}