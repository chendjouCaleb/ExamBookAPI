using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ExamBook.Identity.Services
{
    public class AuthenticationService
    {
        private readonly ApplicationIdentityDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly UserManager<User> _userManager;
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(ApplicationIdentityDbContext dbContext, 
            IPasswordHasher<User> passwordHasher,
            UserService userService, 
            UserManager<User> userManager,
            IConfiguration configuration, 
            ILogger<AuthenticationService> logger)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _userService = userService;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResultModel> LoginPasswordAsync(User user, string password)
        {
            AssertHelper.NotNull(user, nameof(user));
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                throw new IllegalValueException("InvalidUserPassword");
            }

            return await LoginAsync(user);
        }


        public async Task<LoginResultModel> LoginAsync(User user)
        {
            AssertHelper.NotNull(user, nameof(user));

            Console.WriteLine("Hash: " +user.PasswordHash);
            
            Session session = new()
            {
                User = user
            };
            await _dbContext.Sessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();

            var jwtToken = GenerateJwtToken(session);
            _logger.LogInformation("New Login");
            
            return new LoginResultModel(session, jwtToken);
        }


        public async Task LogOut(string sessionId)
        {
            var session = await GetByIdAsync(sessionId);
            session.EndAt = DateTime.Now;
            _dbContext.Update(session);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Session> GetByIdAsync(string id)
        {
            var session = await _dbContext.Sessions
                .Include(s => s.User)
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                throw new ElementNotFoundException("SessionNotFound");
            }
            
            return session;
        }
        
        
        public string GenerateJwtToken(Session session)
        {
            AssertHelper.NotNull(session, nameof(session));
            AssertHelper.NotNull(session.User, nameof(session.User));
            var user = session.User;
            var issuer = _configuration["Jwt:Issuer"];
            //var audience = _configuration["Jwt:Audience"];
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim("SessionId", session.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Name, user.NormalizedUserName!),
                    
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(100000),
                Issuer = issuer,
                Audience = null,
                SigningCredentials = new SigningCredentials
                (new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(jwtToken);
        }
    }
}