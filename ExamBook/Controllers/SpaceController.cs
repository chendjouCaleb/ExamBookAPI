using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
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
        private readonly DbContext _dbContext;

        public SpaceController(SpaceService spaceService, DbContext dbContext)
        {
            _spaceService = spaceService;
            _dbContext = dbContext;
        }


        [HttpGet("{id}")]
        public async Task<Space> FindAsync(string id)
        {
            Space space = await _spaceService.GetAsync(id);
            return space;
        }


        [HttpGet]
        [Authorize]
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
                .Include(s => s.Classroom.Space)
                .Where(m => m.UserId == userId)
                .ToListAsync();

            var spaces = members.Select(m => m.Space)
                .Concat(students.Select(s => s.Classroom.Space))
                .Where(s => s != null)
                .Select(s => s!)
                .DistinctBy(s => s.Id);

            
            return spaces;
        }


        [HttpPost]
        [Authorize]
        public async Task<CreatedAtActionResult> AddSpace(
            [FromBody] SpaceAddModel model)
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var space = await _spaceService.AddAsync(userId, model);

            return CreatedAtAction("Find", new {space.Id}, space);
        }


        [HttpPut("{spaceId}/identifier")]
        [Authorize]
        public Task<StatusCodeResult> ChangeIdentifier(ulong spaceId)
        {
            throw new NotImplementedException();
        }

        
        [HttpPut("{spaceId}/name")]
        [Authorize]
        public Task<StatusCodeResult> ChangeName(ulong spaceId)
        {
            throw new NotImplementedException();
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