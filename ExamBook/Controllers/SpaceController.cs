using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
    
    [Route("api/spaces")]
    public class SpaceController:ControllerBase
    {
        private readonly SpaceService _spaceService;
        private readonly UserService _userService;
        private readonly DbContext _dbContext;

        public SpaceController(SpaceService spaceService, DbContext dbContext, UserService userService)
        {
            _spaceService = spaceService;
            _dbContext = dbContext;
            _userService = userService;
        }


        [HttpGet("{id}")]
        public async Task<Space> FindAsync(ulong id)
        {
            Space space = await _spaceService.GetByIdAsync(id);
            return space;
        }

        
        [HttpGet("get")]
        public async Task<Space> GetAsync([FromQuery] ulong id, [FromQuery] string identifier)
        {
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                return await _spaceService.GetAsync(identifier);
            }
            return await _spaceService.GetByIdAsync(id);
        }


        [HttpGet]
        public async Task<IEnumerable<Space>> List([FromQuery] string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                return await ListByUser(userId);
            }
            IQueryable<Space> query =  _dbContext.Set<Space>();

            return await query.ToListAsync();
        }

       
        private async Task<IEnumerable<Space>> ListByUser(string userId)
        {
            
            var members = await _dbContext.Set<Member>()
                .Include(m => m.Space)
                .Where(m => m.UserId == userId)
                .ToListAsync();
            
            var students = await _dbContext.Set<Student>()
                .Include(s => s.Space)
                .Where(m => m.UserId == userId)
                .ToListAsync();

            var spaces = members.Select(m => m.Space)
                .Concat(students.Select(s => s.Space))
                .Where(s => s != null)
                .Select(s => s!)
                .DistinctBy(s => s.Id);

            
            return spaces;
        }


        [HttpPost]
        [Authorize]
        public async Task<CreatedAtActionResult> AddSpace(
            [FromForm] SpaceAddModel model)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _spaceService.AddAsync(userId, model);
            var space = result.Item;

            return CreatedAtAction("Find", new {space.Id}, space);
        }


        [HttpPut("{spaceId}/identifier")]
        [Authorize]
        public async Task<OkObjectResult> ChangeIdentifier(ulong spaceId, [FromBody] IDictionary<string, string> body)
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var user = await _userService.GetByIdAsync(userId);
            
            string identifier = body["identifier"];
            var space = await _spaceService.GetByIdAsync(spaceId);
            var result = await _spaceService.ChangeIdentifier(space, identifier, user);
            return Ok(result);
        }

        
        [HttpPut("{spaceId}/name")]
        [Authorize]
        public async Task<OkObjectResult> ChangeName(ulong spaceId, [FromBody] IDictionary<string, string> body)
        {
            string name = body["name"];
            
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var user = await _userService.GetByIdAsync(userId);
            
            var space = await _spaceService.GetByIdAsync(spaceId);
            var result = await _spaceService.ChangeName(space, name, user);
            return Ok(result);
        }
        
        [HttpPut("{spaceId}/description")]
        [Authorize]
        public async Task<OkObjectResult> ChangeDescription(ulong spaceId, [FromBody] IDictionary<string, string> body)
        {
            var description = body["description"];
        
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var user = await _userService.GetByIdAsync(userId);
            
            var space = await _spaceService.GetByIdAsync(spaceId);
            var result = await _spaceService.ChangeDescription(space, description, user);
            return Ok(result);
        }


        
        public Task<StatusCodeResult> ChangeImage(ulong spaceId, IFormFile file)
        {
            throw new NotImplementedException();
        }


        [HttpDelete("{spaceId}")]
        public Task<NoContentResult> Delete(ulong spaceId)
        {
            throw new NotImplementedException();
        }
    }
}