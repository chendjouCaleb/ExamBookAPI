using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Identity
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly DbContext _dbContext;

        public UserService(UserManager<User> userManager, ApplicationIdentityDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        


        public async Task<User> AddUserAsync(UserAddModel model)
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

            var result = await _userManager.CreateAsync(user, model.Password);

            return user;
        }

       
        public async Task ChangeUserNameAsync(string id, string userName)
        {
            var user = await FindByIdAsync(id);
            var validator = new UserNameValidator();
            validator.Validate(userName);
            
            
            await _userManager.SetUserNameAsync(user, userName);
        }

        public async Task DeleteAsync(User user)
        {
            await _userManager.DeleteAsync(user);
        }
        
        
        public async Task<List<User>> SelectAllAsync()
        {
            var users = await _dbContext.Set<User>()
                .Select(u => new User
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    CreatedAt = u.CreatedAt,
                    DeletedAt = u.DeletedAt,
                    Deleted = u.Deleted
                })
                .ToListAsync();


            return users;
        }

        public async Task<User> SelectById(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<User> FindByIdAsync(string id)
        {
            var user = await _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
            {
                UserNotFoundException.ThrowNotFoundId(id);
            }

            return user!;
        }


        public async Task<User> FindByUserName(string userName)
        {
            string normalizedUserName = StringHelper.Normalize(userName);
            var user = await _dbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName);

            if (user == null)
            {
                UserNotFoundException.ThrowNotFoundUserName(userName);
            }

            return user!;
        }

        public async Task<bool> ContainsEmail(string email)
        {
            var normalizedEmail = email.Normalize().ToUpper();
            return await _dbContext.Set<User>().AnyAsync(u => normalizedEmail == u.NormalizedEmail);
        }
        
        public async Task<bool> ContainsUserName(string userName)
        {
            var normalizedUserName = userName.Normalize().ToUpper();
            return await _dbContext.Set<User>().AnyAsync(u => normalizedUserName == u.NormalizedUserName);
        }
    }
}