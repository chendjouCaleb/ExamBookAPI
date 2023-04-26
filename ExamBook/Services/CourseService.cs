using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class CourseService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<CourseService> _logger;

        public CourseService(DbContext dbContext, ILogger<CourseService> logger, 
            PublisherService publisherService, 
            EventService eventService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
        }


        public async Task<ActionResultModel<Course>> AddCourseAsync(Space space, CourseAddModel model, User user)
        {
            Asserts.NotNull(model, nameof(model));
            Asserts.NotNull(user, nameof(user));
            string normalizedCode = StringHelper.Normalize(model.Code);
            string normalizedName = StringHelper.Normalize(model.Name);

            if (await ContainsByCode(space, model.Code))
            {
                throw new UsedValueException("CourseCodeUsed");
            }

            if (await ContainsByName(space, model.Name))
            {
                throw new UsedValueException("CourseNameUsed");
            }

            var publisher = await _publisherService.AddAsync();
            Course course = new ()
            {
                Name = model.Name,
                NormalizedName = normalizedName,
                Code = model.Code,
                NormalizedCode = normalizedCode,
                Description = model.Description,
                Coefficient = model.Coefficient,
                Space = space,
                PublisherId = publisher.Id
            };
            await _dbContext.AddAsync(course);

            var courseSpecialities = await _CreateCourseSpecialitiesAsync(course, model.SpecialityIds);
            await _dbContext.AddRangeAsync(courseSpecialities);

            var courseTeachers = await _CreateCourseTeachersCourseAsync(course, model.CourseTeacherAddModels);
            await _dbContext.AddRangeAsync(courseTeachers);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New course");

            var publisherIds = new List<string> {
                publisher.Id, 
                space.PublisherId
            };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_ADD", course);
            return new ActionResultModel<Course>(course, @event);
        }

        public async Task<Event> ChangeCourseCodeAsync(Course course, string code, User user)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(course.Space, nameof(course.Space));

            if (await ContainsByCode(course.Space!, code))
            {
                throw new UsedValueException("CourseCodeUsed");
            }

            var eventData = new ChangeValueData<string>(course.Code, code);

            course.Code = code;
            course.NormalizedCode = StringHelper.Normalize(code);
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                course.PublisherId, 
                course.Space!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_CHANGE_CODE", eventData);
        }

        public async Task<Event> ChangeCourseNameAsync(Course course, string name, User user)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(course.Space, nameof(course.Space));

            if (await ContainsByName(course.Space!, name))
            {
                throw new UsedValueException("CourseNameUsed");
            }

            var eventData = new ChangeValueData<string>(course.Name, name);

            course.Name = name;
            course.NormalizedName = StringHelper.Normalize(name);
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                course.PublisherId, 
                course.Space!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_CHANGE_NAME", eventData);
        }
        
        
        public async Task<Event> ChangeCourseCoefficientAsync(Course course, uint coefficient, User user)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(course.Space, nameof(course.Space));
            var eventData = new ChangeValueData<uint>(course.Coefficient, coefficient);

            course.Coefficient = coefficient;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                course.PublisherId, 
                course.Space!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_CHANGE_COEFFICIENT", eventData);
        }
        
        public async Task<Event> ChangeCourseDescriptionAsync(Course course, string description, User user)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(course.Space, nameof(course.Space));

            var eventData = new ChangeValueData<string>(course.Description, description);

            course.Description = description;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                course.PublisherId, 
                course.Space!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_CHANGE_DESCRIPTION", eventData);
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
                throw new InvalidOperationException($"Member with id={model.MemberId} not found.");
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
            Asserts.NotNull(course.Space, nameof(course.Space));

            // if (course.ClassroomId != classroomSpeciality.ClassroomId)
            // {
            //     throw new IncompatibleEntityException(course, classroomSpeciality);
            // }

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


        public async Task<Course> FindCourseByCodeAsync(Space space, string code)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(code, nameof(code));
            
            string normalizedCode = StringHelper.Normalize(code);
            var course = await _dbContext.Set<Course>()
                .Where(c => c.NormalizedCode == normalizedCode)
                .FirstOrDefaultAsync();

            if (course == null)
            {
                throw new ElementNotFoundException("CourseNotFoundByCode");
            }

            return course;
        }
        
        
        public async Task<Course> FindCourseByNameAsync(Space space, string name)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
            
            string normalizedName = StringHelper.Normalize(name);
            var course = await _dbContext.Set<Course>()
                .Where(c => c.NormalizedName == normalizedName)
                .FirstOrDefaultAsync();

            if (course == null)
            {
                throw new ElementNotFoundException("CourseNotFoundByName");
            }

            return course;
        }
        
        public async Task<bool> ContainsByCode(Space space, string code)
        {
            Asserts.NotNull(space, nameof(space));
            
            string normalizedCode = StringHelper.Normalize(code);
            return await _dbContext.Set<Course>()
                .Where(c => c.NormalizedCode == normalizedCode)
                .Where(c => c.SpaceId == space.Id)
                .AnyAsync();
        }
        
        public async Task<bool> ContainsByName(Space space, string name)
        {
            Asserts.NotNull(space, nameof(space));
            
            string normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Set<Course>()
                .Where(c => c.NormalizedName == normalizedName)
                .Where(c => c.SpaceId == space.Id)
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
                throw new InvalidOperationException($"Member with id={memberId} not found.");
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

        
        public async Task<Event> DeleteAsync(Course course, User user)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(user, nameof(user));

            course.Name = "";
            course.NormalizedName = "";
            course.Code = "";
            course.NormalizedCode = "";
            course.Description = "";
            course.Coefficient = 0;
            course.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { course.Space!.PublisherId, course.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_DELETE", course);
        }

        public async Task<Event> DestroyAsync(Course course, User user)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(user, nameof(user));

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

            var publisherIds = new List<string> { course.Space!.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_DESTROY", course);
        }

    }
}