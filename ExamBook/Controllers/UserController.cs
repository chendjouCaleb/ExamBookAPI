using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ExamBook.Helpers;
using ExamBook.Identity;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
    [Route("api/users")]
    public class UserController: ControllerBase
    {
        private readonly UserService _userService;
        private readonly UserCodeService _userCodeService;
        private readonly ApplicationIdentityDbContext _dbContext;

        public UserController(UserService userService,
            ApplicationIdentityDbContext dbContext, 
            UserCodeService userCodeService)
        {
            _userService = userService;
            _dbContext = dbContext;
            _userCodeService = userCodeService;
        }

        [HttpGet]
        public async Task<IEnumerable<User>> List()
        {
            return await _dbContext.Users.ToListAsync();
        }
        
        [HttpGet("{id}")]
        public async Task<User> GetAsync(string id)
        {
            return await _userService.GetByIdAsync(id);
        }
        
        [HttpGet("get")]
        public async Task<User> GetEmailAsync([FromQuery] string email, [FromQuery] string userName,
            [FromQuery] string id)
        {
            if(!string.IsNullOrWhiteSpace(email))
                return await _userService.GetByEmailAsync(email);
            
            if(!string.IsNullOrWhiteSpace(userName))
                return await _userService.GetByUserNameAsync(userName);
            
            return await _userService.GetByIdAsync(id);
        }
        
       
        
        
        [HttpGet("contains")]
        public async Task<bool> ContainsAsync([FromQuery] string userId,
            [FromQuery] string email,
            [FromQuery] string userName,
            [FromQuery] string id)
        {
            if(!string.IsNullOrWhiteSpace(email))
                return await _userService.ContainsByEmailAsync(email);
            
            if(!string.IsNullOrWhiteSpace(userName))
                return await _userService.ContainsByUserNameAsync(userName);
            
            if(!string.IsNullOrWhiteSpace(id))
                return await _userService.ContainsByIdAsync(id);
            
            return await _userService.ContainsByUserNameOrEmailAsync(userId);
        }


        [HttpPost]
        public async Task<User> AddUserAsync([FromForm] UserAddModel model) 
        {
            AssertHelper.NotNull(model, nameof(model));
            
            var userCode = await _userCodeService.GetAsync(model.Email, "CreateAccount");
            _userCodeService.CheckCode(userCode, model.Code);
            
            var user = await _userService.AddUserAsync(model);
            return user;
        }
        
        
        [HttpPut("change-password")]
        public async Task<StatusCodeResult> ChangePassword([FromQuery] string userId,[FromBody] ChangePasswordModel model)
        {
            var user = await _userService.GetAsync(userId);
            await _userService.ChangePassword(user, model);
            return Ok();
        }
        
        

        [HttpPost("code")]
        public async Task<StatusCodeResult> GenerateCode([FromBody] CreateCodeModel model)
        {
            AssertHelper.NotNull(model, nameof(model));
            await _userCodeService.AddOrUpdateCodeAsync(model.UserId, model.Purpose);
            return Ok();
        }
        
        [HttpPost("check-code")]
        public async Task<OkObjectResult> CheckCode([FromBody] CheckCodeModel model)
        {
            
            var isValid = await _userCodeService.IsValidCode(model.UserId, model.Purpose, model.Code);
            return Ok(isValid);
        }

        [HttpPut("reset-password")]
        public async Task<StatusCodeResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            await _userService.ResetPassword(model);
            return NoContent();
        }

        
        [HttpPut("check-password")]
        public async Task<OkObjectResult> CheckUserPassword([FromBody]CheckPasswordModel model)
        {
            var user = await _userService.GetAsync(model.UserId);
            var isValid = _userService.CheckPassword(user, model.Password);
            return Ok(isValid);
        }
        

        [HttpPut("{userId}/userName")]
        public async Task<Ok> ChangeUserName(string userId, string userName)
        {
            await _userService.ChangeUserNameAsync(userId, userName);
            return TypedResults.Ok();
        }
    }
}