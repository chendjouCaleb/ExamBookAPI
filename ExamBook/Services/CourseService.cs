using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamBook.Services
{
    public class CourseService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<CourseService> _logger;

        public CourseService(DbContext dbContext, ILogger<CourseService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task<Course> AddCourseAsync(Space space, CourseAddModel model)
        {
            string normalizedCode = StringHelper.Normalize(model.Code);
            string normalizedName = StringHelper.Normalize(model.Name);

            var classroom = await _dbContext.Set<Classroom>()
                .FindAsync(model.ClassroomId);
            
            Course course = new ()
            {
                Name = model.Name,
                NormalizedName = normalizedName,
                Code = model.Code,
                NormalizedCode = normalizedCode,
                Description = model.Description,
                Coefficient = model.Coefficient,
                Classroom = classroom
            };
            await _dbContext.AddAsync(course);

            var courseSpecialities = await _CreateCourseSpecialitiesAsync(course, model.SpecialityIds);
            await _dbContext.AddRangeAsync(courseSpecialities);

            var courseTeachers = await _CreateCourseTeachersCourseAsync(course, model.CourseTeacherAddModels);
            await _dbContext.AddRangeAsync(courseTeachers);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New course");
            return course;
        }

        public async Task ChangeCourseCode(Course course, string code)
        {
            Asserts.NotNull(course, nameof(course));
            var space = await _dbContext
                .Set<Space>()
                .FindAsync(course.Classroom!.SpaceId);

            if (await ContainsByCode(space, code))
            {
                CourseHelper.ThrowDuplicateCode(code);
            }

            course.Code = code;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ChangeCourseName(Course course, string name)
        {
            Asserts.NotNull(course, nameof(course));

            var classroom = await _dbContext.Set<Classroom>().FindAsync(course.ClassroomId);
            
            Asserts.NotNull(classroom, nameof(classroom));
            
            if (await ContainsByName(classroom, name))
            {
                CourseHelper.ThrowDuplicateCode(name);
            }

            course.Name = name;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
        }
        
        
        public async Task ChangeCourseCoefficient(Course course, uint coefficient)
        {
            Asserts.NotNull(course, nameof(course));

            course.Coefficient = coefficient;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
        }
        
        public async Task ChangeCourseDescription(Course course, string description)
        {
            Asserts.NotNull(course, nameof(course));

            course.Description = description;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
        }


        public async Task<CourseTeacher> AddCourseTeachers(Course course, Member member)
        {
            Asserts.NotNull(member, nameof(member));
            Asserts.NotNull(course, nameof(course));
            CourseTeacherAddModel model = new()
            {
                MemberId = member.Id
            };
            var courseTeacher = await _CreateCourseTeacherAsync(course, model);
            await _dbContext.AddAsync(courseTeacher);
            await _dbContext.SaveChangesAsync();
            return courseTeacher;
        }

        public async Task<List<CourseTeacher>> _CreateCourseTeachersCourseAsync(Course course, List<CourseTeacherAddModel> models)
        {
            var courseTeachers = new List<CourseTeacher>();

            foreach (var model in models)
            {
                if (await CourseTeacherExists(course, model.MemberId))
                {
                    var courseTeacher = await _CreateCourseTeacherAsync(course, model);
                    courseTeachers.Add(courseTeacher);
                }
            }

            return courseTeachers;
        }

        public async Task<CourseTeacher> _CreateCourseTeacherAsync(Course course, CourseTeacherAddModel model)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(model, nameof(model));

            var member = await _dbContext.Set<Member>().FindAsync(model.MemberId);

            if (member == null)
            {
                
            }

            if (!member.IsTeacher)
            {
                
            }

            CourseTeacher courseTeacher = new()
            {
                IsPrincipal = model.IsPrincipal,
                Course = course,
                Member = member
            };

            return courseTeacher;
        }


        public async Task<List<CourseSpeciality>> _CreateCourseSpecialitiesAsync(Course course, 
            IEnumerable<ulong> classroomSpecialityIds)
        {
            var courseSpecialities = new List<CourseSpeciality>();
            foreach (var classroomSpecialityId in classroomSpecialityIds)
            {
                var classroomSpeciality = await _dbContext.Set<ClassroomSpeciality>().FindAsync(classroomSpecialityId);
                if (classroomSpeciality == null)
                {
                    throw new InvalidOperationException($"Classroom speciality with id={classroomSpecialityId} not found.");
                }

                if (!await CourseSpecialityExists(course, classroomSpeciality))
                {
                    var courseSpeciality = await _CreateCourseSpecialityAsync(course, classroomSpeciality);
                    courseSpecialities.Add(courseSpeciality);
                }
            }

            return courseSpecialities;
        }

        public async Task<CourseSpeciality> _CreateCourseSpecialityAsync(Course course, 
            ClassroomSpeciality classroomSpeciality)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNull(course.Classroom, nameof(course.Classroom));

            if (course.ClassroomId != classroomSpeciality.ClassroomId)
            {
                throw new IncompatibleEntityException(course, classroomSpeciality);
            }

            if (await CourseSpecialityExists(course, classroomSpeciality))
            {
                CourseHelper.ThrowDuplicateCourseSpeciality(course, classroomSpeciality);
            }


            CourseSpeciality courseSpeciality = new()
            {
                Course = course,
                ClassroomSpeciality = classroomSpeciality
            };
            return courseSpeciality;
        }


        public async Task<Course?> FindByCode(Space space, string code)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(code, nameof(code));
            
            string normalizedCode = StringHelper.Normalize(code);
            return await _dbContext.Set<Course>()
                .Where(c => c.NormalizedCode == normalizedCode)
                .FirstOrDefaultAsync();
        }
        
        public async Task<bool> ContainsByCode(Space space, string code)
        {
            Asserts.NotNull(space, nameof(space));
            
            string normalizedCode = StringHelper.Normalize(code);
            return await _dbContext.Set<Course>()
                .Where(c => c.NormalizedCode == normalizedCode)
                .Where(c => c.Classroom!.SpaceId == space.Id)
                .AnyAsync();
        }
        
        public async Task<bool> ContainsByName(Classroom classroom, string name)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            
            string normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Set<Course>()
                .Where(c => c.NormalizedName == normalizedName)
                .Where(c => c.ClassroomId == classroom.Id)
                .AnyAsync();
        }

        public async Task<bool> CourseTeacherExistsAsync(Course course, Member member)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(member, nameof(member));

            return await _dbContext.Set<CourseTeacher>()
                .Where(ct => ct.CourseId == course.Id)
                .Where(ct => ct.MemberId == member.Id)
                .AnyAsync();
        }

        public async Task<bool> CourseTeacherExists(Course course, ulong memberId)
        {
            var member = await _dbContext.Set<Member>().FindAsync(memberId);
            if (member == null)
            {
                
            }
            return await CourseTeacherExistsAsync(course, member);
        }

        public async Task<bool> CourseSpecialityExists(Course course, Speciality speciality)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(speciality, nameof(speciality));

            return await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseId == course.Id)
                .Where(cs => cs.ClassroomSpeciality!.SpecialityId == speciality.Id)
                .AnyAsync();
        }
        
        public async Task<bool> CourseSpecialityExists(Course course, ClassroomSpeciality classroomSpeciality)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));

            return await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseId == course.Id)
                .Where(cs => cs.ClassroomSpecialityId == classroomSpeciality.Id)
                .AnyAsync();
        }


        public async Task DeleteCourseTeacher(CourseTeacher courseTeacher)
        {
            Asserts.NotNull(courseTeacher, nameof(courseTeacher));
            _dbContext.Remove(courseTeacher);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveCourseSpeciality(CourseSpeciality courseSpeciality)
        {
            Asserts.NotNull(courseSpeciality, nameof(courseSpeciality));
            _dbContext.Remove(courseSpeciality);
            await _dbContext.SaveChangesAsync();
        }


        public async Task Delete(Course course)
        {
            Asserts.NotNull(course, nameof(course));

            var courseSpecialities = await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseId == course.Id)
                .ToListAsync();
            
            var courseTeachers = await _dbContext.Set<CourseTeacher>()
                .Where(cs => cs.CourseId == course.Id)
                .ToListAsync();
            
            _dbContext.RemoveRange(courseSpecialities);
            _dbContext.RemoveRange(courseTeachers);
            _dbContext.Remove(course);
            await _dbContext.SaveChangesAsync();
        }

    }
}