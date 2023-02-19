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


        [Route("{identifier}")]
        public async Task<Space> Get(string identifier)
        {
            Space space = await _spaceService.GetAsync(identifier);
            return space;
        }


        public async Task<IEnumerable<Space>> List()
        {
            IQueryable<Space> query =  _dbContext.Set<Space>();

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Space>> ListByUser(string userId)
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
                .DistinctBy(s => s.Id);

            return spaces;
        }
    }
}