using System;
using System.Threading.Tasks;
using ExamBook.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Identity
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly DbContext _dbContext;

        public UserService(UserManager<User> userManager, DbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }


        public async Task<User> AddUser(UserAddModel model)
        {
            if (await ContainsUserName(model.UserName))
            {
                
            }

            User user = new ()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Sex = model.Sex,
                UserName = model.UserName,
                BirthDate = model.BirthDate!.Value,
                CreatedAt = DateTime.UtcNow
            };

            await _userManager.CreateAsync(user, model.Password);

            return user;
        }

       
        public async Task ChangeUserNameAsync(User user, string userName)
        {
            await _userManager.SetUserNameAsync(user, userName);
        }

        public async Task DeleteAsync(User user)
        {
            await _userManager.DeleteAsync(user);
        }

        public async Task<bool> ContainsEmail(string email)
        {
            var normalizedEmail = email.Normalize().ToUpper();
            return await _dbContext.Set<User>().AnyAsync(u => normalizedEmail.Equals(u.NormalizedEmail));
        }
        
        public async Task<bool> ContainsUserName(string userName)
        {
            var normalizedUserName = userName.Normalize().ToUpper();
            return await _dbContext.Set<User>().AnyAsync(u => normalizedUserName.Equals(u.NormalizedUserName));
        }
    }
}