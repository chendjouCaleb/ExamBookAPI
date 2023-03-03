using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Http
{
    [Route("api/examinations")]
    public class ExaminationControllerApi:ControllerBase
    {
        
        private readonly DbContext _dbContext;

        public ExaminationControllerApi(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Examination> Get(long id)
        {
            var examination = await _dbContext.Set<Examination>().FindAsync(id);
            if (examination == null)
            {
                throw new NullReferenceException();
            }
            return examination;
        }

        public async Task<List<Examination>> List([FromQuery] ulong? spaceId)
        {
            IQueryable<Examination> query = _dbContext.Set<Examination>();

            if (spaceId != null && spaceId != 0)
            {
                query = query.Where(e => e.SpaceId == spaceId);
            }

            return await query.ToListAsync();
        }
    }
}