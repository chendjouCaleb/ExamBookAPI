using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Entities.School;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamBook.Services
{
    public class SpecialityService
    {
        private DbContext _dbContext;
        private ILogger<SpecialityService> _logger;

        public SpecialityService(DbContext dbContext, ILogger<SpecialityService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task<Speciality> AddSpeciality(Space space, SpecialityAddModel model)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNull(model, nameof(space));

            if (await ContainsAsync(space, model.Name))
            {
                SpaceHelper.ThrowDuplicateSpeciality();
            }
            var normalizedName = model.Name.Normalize().ToUpper();
            Speciality speciality = new()
            {
                Space = space,
                Name = model.Name,
                NormalizedName = normalizedName
            };

            await _dbContext.AddAsync(speciality);
            await _dbContext.SaveChangesAsync();
            string m = string.Format("New speciality created in space: {0}.", space.Name);
            _logger.LogInformation("New speciality created");
            return speciality;
        }
        
        
        public async Task<bool> ContainsAsync(Space space, string name)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
            var normalized = name.Normalize().ToUpper();
            return await  _dbContext.Set<Speciality>()
                .AnyAsync(s => space.Equals(s.Space) && s.NormalizedName == normalized);
        }
        
        public async Task<Speciality?> FindAsync(Space space, string name)
        {
            var normalized = name.Normalize().ToUpper();
            return await _dbContext.Set<Speciality>()
                .Where(s => space.Equals(s.Space) && normalized == s.Name)
                .FirstOrDefaultAsync();
        }


        public async Task Delete(Speciality speciality)
        {
            Asserts.NotNull(speciality, nameof(speciality));
            var classroomSpecialities = _dbContext.Set<ClassroomSpeciality>()
                .Where(cs => speciality.Equals(cs.Speciality));

            _dbContext.RemoveRange(classroomSpecialities);
            _dbContext.Remove(speciality);
            await _dbContext.SaveChangesAsync();
        }
    }
}