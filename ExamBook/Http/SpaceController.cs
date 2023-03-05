using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Models;
using ExamBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Http
{
    
    [Route("api/spaces")]
    public class SpaceController:ControllerBase
    {
        private SpaceService _spaceService;
        private DbContext _dbContext;

        public SpaceController(SpaceService spaceService, DbContext dbContext)
        {
            _spaceService = spaceService;
            _dbContext = dbContext;
        }


        [HttpGet("{identifier}")]
        public async Task<Space> Get(string identifier)
        {
            Space space = await _spaceService.GetAsync(identifier);
            return space;
        }


        [HttpGet]
        public async Task<IEnumerable<Space>> List([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
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



        public async Task<CreatedAtActionResult> AddSpace([FromBody] SpaceAddModel model)
        {
            throw new NotImplementedException();
        }


        public async Task<StatusCodeResult> ChangeIdentifier(ulong spaceId)
        {
            throw new NotImplementedException();
        }

        public async Task<StatusCodeResult> ChangeName(ulong spaceId)
        {
            throw new NotImplementedException();
        }

        public async Task<NoContentResult> Delete(ulong spaceId)
        {
            throw new NotImplementedException();
        }
    }
}