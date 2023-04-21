using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Services;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Identity
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly AuthorService _authorService;
        private readonly ActorService _actorService;
        private readonly PublisherService _publisherService;
        private readonly ApplicationIdentityDbContext _dbContext;

        public UserService(UserManager<User> userManager, 
            ApplicationIdentityDbContext dbContext, 
            AuthorService authorService, 
            ActorService actorService, 
            PublisherService publisherService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _authorService = authorService;
            _actorService = actorService;
            _publisherService = publisherService;
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
            if (!result.Succeeded)
            {
                foreach (var identityError in result.Errors)
                {
                    Console.Error.WriteLine(identityError.Code + ": " + identityError.Description);
                }
            }

            var author = await _authorService.AddAuthorAsync(user.UserName);
            var actor = await _actorService.AddAsync();
            var publisher = await _publisherService.AddAsync();
            
            user.AuthorId = author.Id;
            user.ActorId = actor.Id;
            user.PublisherId = publisher.Id;
            await _userManager.UpdateAsync(user);

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

        public async Task<Author> GetAuthor(User user)
        {
            if (string.IsNullOrWhiteSpace(user.AuthorId))
            {
                throw new IllegalStateException("UserHasNoAuthor");
            }
            return await _authorService.GetByIdAsync(user.AuthorId);
        }
        
        
        public async Task<Actor> GetActor(User user)
        {
            if (string.IsNullOrWhiteSpace(user.ActorId))
            {
                throw new IllegalStateException("UserHasNoActor");
            }

            return await _actorService.GetByIdAsync(user.ActorId);
        }
        
        
        public async Task<Publisher> GetPublisher(User user)
        {
            if (string.IsNullOrWhiteSpace(user.PublisherId))
            {
                throw new IllegalStateException("UserHasNoPublisher");
            }

            return await _publisherService.GetByIdAsync(user.PublisherId);
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