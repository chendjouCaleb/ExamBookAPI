using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Exceptions;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using ExamBook.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Social.Entities;
using Social.Services;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Identity.Services
{
	public class UserService
	{
		private readonly UserManager<User> _userManager;
		private readonly IPasswordHasher<User> _passwordHasher;
		private readonly UserCodeService _userCodeService;
		private readonly AuthorService _authorService;
		private readonly ActorService _actorService;
		private readonly PublisherService _publisherService;
		private readonly ApplicationIdentityDbContext _dbContext;

		public UserService(UserManager<User> userManager,
			ApplicationIdentityDbContext dbContext,
			AuthorService authorService,
			ActorService actorService,
			PublisherService publisherService, IPasswordHasher<User> passwordHasher, UserCodeService userCodeService)
		{
			_userManager = userManager;
			_dbContext = dbContext;
			_authorService = authorService;
			_actorService = actorService;
			_publisherService = publisherService;
			_passwordHasher = passwordHasher;
			_userCodeService = userCodeService;
		}
		
		public async Task<ImmutableList<User>> ListById(ICollection<string> userId)
		{
			var users = await _dbContext.Users.Where(u => userId.Contains(u.Id)).ToListAsync();
			return users.ToImmutableList();
		}


		public async Task<User> GetAsync(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentNullException(nameof(id));
			}

			var normalized = StringHelper.Normalize(id);
			var user = await _dbContext.Set<User>()
				.Where(u => u.Id == normalized || u.NormalizedUserName == normalized || u.NormalizedEmail == normalized)
				.FirstOrDefaultAsync();

			if (user == null)
			{
				throw new ElementNotFoundException("UserNotFound", id);
			}

			return user;
		}


		public async Task<User> GetByIdAsync(string userId)
		{
			//var user = await _userManager.FindByIdAsync(userId);
			var user = await _dbContext.Set<User>()
				.Where(u => u.Id == userId)
				.FirstOrDefaultAsync();

			if (user == null)
			{
				throw new ElementNotFoundException("UserNotFoundById", userId);
			}

			return user;
		}


		public async Task<User> GetByUserNameOrEmailAsync(string id)
		{
			var normalized = StringHelper.Normalize(id);
			var user = await _dbContext.Set<User>()
				.Where(u => u.NormalizedUserName == normalized || u.NormalizedEmail == normalized)
				.FirstOrDefaultAsync();

			if (user == null)
			{
				throw new ElementNotFoundException("UserNotFoundByUserNameOrEmail", id);
			}

			return user;
		}

		public async Task<User> GetByUserNameAsync(string userName)
		{
			var user = await _userManager.FindByNameAsync(userName);
			if (user == null)
			{
				throw new ElementNotFoundException("UserNotFoundByUserName", userName);
			}

			return user;
		}

		public async Task<User> GetByEmailAsync(string email)
		{
			var user = await _userManager.FindByEmailAsync(email);
			if (user == null)
			{
				throw new ElementNotFoundException("UserNotFoundByEmail", email);
			}

			return user;
		}

		public async Task<bool> ContainsByUserNameAsync(string userName)
		{
			var normalized = StringHelper.Normalize(userName);
			return await _dbContext.Users.Where(u => u.NormalizedUserName == normalized)
				.AnyAsync();
		}

		public async Task<bool> ContainsByEmailAsync(string email)
		{
			var normalized = StringHelper.Normalize(email);
			return await _dbContext.Users.Where(u => u.NormalizedEmail == normalized)
				.AnyAsync();
		}

		public async Task<bool> ContainsByIdAsync(string userId)
		{
			return await _dbContext.Users.Where(u => u.Id == userId)
				.AnyAsync();
		}

		public async Task<bool> ContainsByUserNameOrEmailAsync(string id)
		{
			var normalized = StringHelper.Normalize(id);
			return await _dbContext.Set<User>()
				.Where(u => u.NormalizedUserName == normalized || u.NormalizedEmail == normalized)
				.AnyAsync();
		}

		public async Task<User> AddUserAsync(UserAddModel model)
		{
			if (await ContainsByEmailAsync(model.Email))
			{
				throw new UsedValueException("UserEmailUsed", model.Email);
			}
			
			if (await ContainsUserName(model.UserName))
			{
				throw new UsedValueException("UserNameUsed", model.UserName);
			}

			User user = new()
			{
				Email = model.Email,
				FirstName = model.FirstName,
				LastName = model.LastName,
				Sex = model.Sex,
				UserName = model.UserName,
				BirthDate = model.BirthDate!.Value,
				CreatedAt = DateTime.UtcNow
			};

			var result = await _userManager.CreateAsync(user, model.Password);
			IdentityResultException.ThrowIfError(result);

			var author = await _authorService.AddAuthorAsync(user.UserName);
			var actor = await _actorService.AddAsync();
			var publisher = await _publisherService.AddAsync();

			user.AuthorId = author.Id;
			user.ActorId = actor.Id;
			user.PublisherId = publisher.Id;
			await _userManager.UpdateAsync(user);

			return user;
		}

		public async Task EnsureVx()
		{
			var users = await _dbContext.Users.ToListAsync();
			foreach (var user in users)
			{
				if (string.IsNullOrWhiteSpace(user.PublisherId))
				{
					var publisher = await _publisherService.AddAsync();
					user.PublisherId = publisher.Id;
				}
				
				if (string.IsNullOrWhiteSpace(user.ActorId))
				{
					var actor = await _actorService.AddAsync();
					user.ActorId = actor.Id;
				}

				_dbContext.Update(user);
				await _dbContext.SaveChangesAsync();
			}
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

		public bool CheckPassword(User user, string password)
		{
			AssertHelper.NotNull(user, nameof(user));
			var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);

			if (result == PasswordVerificationResult.Success)
			{
				return true;
			}

			return false;
		}

		public async Task ChangePassword(User user, ChangePasswordModel model)
		{
			AssertHelper.NotNull(user, nameof(user));
			AssertHelper.NotNull(model, nameof(model));

			if (PasswordVerificationResult.Success !=
			    _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, model.CurrentPassword))
			{
				throw new IllegalValueException("InvalidCurrentPassword");
			}

			var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

			if (!result.Succeeded)
			{
				throw new InvalidOperationException(
					$"Error during change password:\n {JsonConvert.SerializeObject(result.Errors)}");
			}
		}


		public async Task ResetPassword(ResetPasswordModel model)
		{
			AssertHelper.NotNull(model, nameof(model));

			var user = await GetAsync(model.UserId);
			var userCode = await _userCodeService.GetAsync(model.UserId, "ResetPassword");
			_userCodeService.CheckCode(userCode, model.Code);

			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

			IdentityResultException.ThrowIfError(result);
			await _userCodeService.DeleteAsync(userCode);
		}

		
	}
}