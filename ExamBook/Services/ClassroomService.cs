using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Entities.School;
using ExamBook.Exceptions;
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

        public ClassroomService(DbContext dbContext, ILogger<ClassroomService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


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
            classroom.ClassroomSpecialities = await CreateClassroomSpecialitiesAsync(classroom, model.SpecialityIds);

            await _dbContext.AddAsync(classroom);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("New classroom {} in space: {}", classroom.Name, space.Name);
            return classroom;
        }


        public async Task<List<ClassroomSpeciality>> AddClassroomSpecialitiesAsync(
            Classroom classroom,
            List<ulong> specialityIds)
        {
            var classroomSpecialities = await CreateClassroomSpecialitiesAsync(classroom, specialityIds);
            await _dbContext.AddRangeAsync(classroomSpecialities);
            await _dbContext.SaveChangesAsync();
            return classroomSpecialities;
        }


        public async Task<ClassroomSpeciality> AddSpeciality(Classroom classroom, Speciality speciality)
        {
            var classroomSpeciality = await CreateSpecialityAsync(classroom, speciality);
            await _dbContext.AddAsync(classroomSpeciality);
            await _dbContext.SaveChangesAsync();
            return classroomSpeciality;
        }

        public async Task<List<ClassroomSpeciality>> CreateClassroomSpecialitiesAsync(Classroom classroom,
            List<ulong> specialityIds)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNull(specialityIds, nameof(specialityIds));

            var specialities = await _dbContext.Set<Speciality>()
                .Where(s => specialityIds.Contains(s.Id))
                .ToListAsync();

            var classroomSpecialities = new List<ClassroomSpeciality>();

            foreach (var speciality in specialities)
            {
                var classroomSpeciality = await CreateSpecialityAsync(classroom, speciality);
                classroomSpecialities.Add(classroomSpeciality);
            }

            return classroomSpecialities;
        }

        public async Task<ClassroomSpeciality> CreateSpecialityAsync(Classroom classroom, Speciality speciality)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNull(speciality, nameof(speciality));
            Asserts.NotNull(classroom.Space, nameof(classroom.Space));

            if (!classroom.Space!.Equals(speciality.Space))
            {
                throw new IncompatibleEntityException(classroom, speciality);
            }

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
            return await _dbContext.Set<Classroom>()
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