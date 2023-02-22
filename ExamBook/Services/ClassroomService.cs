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
    public class ClassroomService
    {
        private DbContext _dbContext;
        private ILogger<ClassroomService> _logger;


        public async Task<Classroom> AddClassroom(Space space, ClassroomAddModel model)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNull(model, nameof(model));

            if (!await ContainsAsync(space, model.Name))
            {
                SpaceHelper.ThrowDuplicateSpeciality();
            }

            Classroom classroom = new()
            {
                Space = space,
                Name = model.Name
            };

           await _dbContext.AddAsync(classroom);
           await _dbContext.SaveChangesAsync();

           return classroom;
        }


        public async Task<Classroom> AddSpeciality(Classroom classroom, Speciality speciality)
        {
            
        }
        
        
        
        public async Task<ClassroomSpeciality> CreateSpecialityAsync(Classroom classroom, Speciality speciality)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNull(speciality, nameof(speciality));

            if (await ContainsSpecialityAsync(classroom, speciality))
            {
                SpaceHelper.ThrowDuplicateClassroomSpeciality();
            }

            ClassroomSpeciality classroomSpeciality = new()
            {
                Classroom = classroom,
                Speciality = speciality
            };

            await _dbContext.AddAsync(classroomSpeciality);
            await _dbContext.SaveChangesAsync();
            return classroomSpeciality;
        }

        public async Task<bool> ContainsSpecialityAsync(Classroom classroom, Speciality speciality)
        {
            return await _dbContext.Set<ClassroomSpeciality>()
                .Where(cs => classroom.Equals(cs.Classroom) && speciality.Equals(cs.Speciality))
                .AnyAsync();
        }

        public async Task<bool> ContainsAsync(Space space, string name)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
            var normalized = name.Normalize().ToUpper();
            return await  _dbContext.Set<Classroom>()
                .AnyAsync(c => space.Equals(c.Space) && c.NormalizedName == normalized);
        }
        
        
        public async Task<Classroom?> FindAsync(Space space, string name)
        {
            var normalized = name.Normalize().ToUpper();
            return await _dbContext.Set<Classroom>()
                .Where(c => space.Equals(c.Space) && normalized == c.Name)
                .FirstOrDefaultAsync();
        }
        
        
    }
}