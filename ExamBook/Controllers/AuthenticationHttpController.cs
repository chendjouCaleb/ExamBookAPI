using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ExamBook.Helpers;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ExamBook.Controllers
{
    
    [Route("api/auth")]
    public class AuthenticationHttpController:ControllerBase
    {
        private ApplicationIdentityDbContext _dbContext;
        private ILogger<AuthenticationHttpController> _logger;
        private IPasswordHasher<User> _passwordHasher;
        private IConfiguration _configuration;

        public AuthenticationHttpController(ApplicationIdentityDbContext dbContext,
            ILogger<AuthenticationHttpController> logger, 
            IPasswordHasher<User> passwordHasher, 
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<object> LoggedUser()
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return userId;
        }

        [HttpPost]
        public async Task<OkObjectResult> Login([FromBody] LoginModel model)
        {
            Asserts.NotNull(model, nameof(model));

            var normalizedUserName = StringHelper.Normalize(model.Id);
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserName == normalizedUserName);

            if (user == null)
            {
                throw new InvalidOperationException($"User with userName={model.Id} not found.");
            }
            
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Name, user.NormalizedUserName),
                  
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
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);
            var stringToken = tokenHandler.WriteToken(token);
            return Ok(stringToken);
            
        }
    }
}