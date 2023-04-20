using System.Collections.Generic;
using System.Threading.Tasks;
using ExamBook.Helpers;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
    [Route("api/users")]
    public class UserController
    {
        private readonly UserService _userService;
        private readonly ApplicationIdentityDbContext _dbContext;

        public UserController(UserService userService, ApplicationIdentityDbContext dbContext)
        {
            _userService = userService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IEnumerable<User>> List()
        {
            return await _dbContext.Users.ToListAsync();
        }


        [HttpGet("{id}")]
        public async Task<User> Get(string id)
        {
            if (StringHelper.IsGuid(id))
            {
                return await _userService.FindByIdAsync(id);
            }

            return await _userService.FindByUserName(id);
        }


        [HttpPost]
        public async Task<IResult> AddUser([FromBody] UserAddModel model)
        {
            Asserts.NotNull(model, nameof(model));
            var user = await _userService.AddUserAsync(model);
            

            return TypedResults.CreatedAtRoute(user, "Get", new {user.Id});
        }


        [HttpPut("{userId}/userName")]
        public async Task<Ok> ChangeUserName(string userId, string userName)
        {
            await _userService.ChangeUserNameAsync(userId, userName);
            return TypedResults.Ok();
        }
    }
}