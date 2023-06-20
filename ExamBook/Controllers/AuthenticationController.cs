using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Identity;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamBook.Controllers
{
    
    [Route("api/authentication")]
    public class AuthenticationController:ControllerBase
    {
        private readonly ApplicationIdentityDbContext _dbContext;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly AuthenticationService _authenticationService;
        private readonly UserService _userService;
        
        public AuthenticationController(ApplicationIdentityDbContext dbContext,
            ILogger<AuthenticationController> logger, 
            IPasswordHasher<User> passwordHasher, 
            AuthenticationService authenticationService, UserService userService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _authenticationService = authenticationService;
            _userService = userService;
        }

        [HttpGet("userId")]
        public string LoggedUserId()
        {
            return HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        }
        
        [HttpGet("user")]
        public async Task<User> LoggedUser()
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            return await _userService.GetByIdAsync(userId);

        }
        
        [HttpGet("sessionId")]
        public string LoggedSessionId()
        {
            return HttpContext.User.FindFirst("SessionId")!.Value;
        }

        [HttpGet("session")]
        public async Task<Session> LoggedSession()
        {
            var sessionId = HttpContext.User.FindFirst("SessionId")!.Value;
            return await _authenticationService.GetByIdAsync(sessionId);
        }

        [HttpPost("login")]
        public async Task<OkObjectResult> Login([FromBody] LoginModel model)
        {
            AssertHelper.NotNull(model, nameof(model));
            var user = await _userService.GetAsync(model.UserId);
            var result = await _authenticationService.LoginPasswordAsync(user, model.Password);

            return Ok(result);
        }
    }
}