using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
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
        private readonly MemberService _memberService;
        private readonly EventService _eventService;
        private readonly ILogger<CourseService> _logger;

        public CourseService(DbContext dbContext, ILogger<CourseService> logger, 
            PublisherService publisherService, 
            EventService eventService, MemberService memberService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
            _memberService = memberService;
        }
        
        
        /// <summary>
        /// Get course by id.
        /// </summary>
        /// <param name="id">The id of course.</param>
        /// <returns>Course corresponding to the provided id.</returns>
        /// <exception cref="ElementNotFoundException">If course not found.</exception>
        public async Task<Course> GetCourseAsync(ulong id)
        {
            var course = await _dbContext.Set<Course>()
                .Where(c => c.Id == id)
                .Include(c => c.Space)
                .FirstOrDefaultAsync();

            if (course == null)
            {
                throw new ElementNotFoundException("CourseNotFoundById", id);
            }

            return course;
        }

        public async Task<Course> GetCourseByCodeAsync(Space space, string code)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));
            
            string normalizedCode = StringHelper.Normalize(code);
            var course = await _dbContext.Set<Course>()
                .Include(c => c.Space)
                .Where(c => c.NormalizedCode == normalizedCode)
                .FirstOrDefaultAsync();

            if (course == null)
            {
                throw new ElementNotFoundException("CourseNotFoundByCode", code);
            }

            return course;
        }
        
        
        public async Task<Course> GetCourseByNameAsync(Space space, string name)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNullOrWhiteSpace(name, nameof(name));
            
            string normalizedName = StringHelper.Normalize(name);
            var course = await _dbContext.Set<Course>()
                .Include(c => c.Space)
                .Where(c => c.NormalizedName == normalizedName)
                .FirstOrDefaultAsync();

            if (course == null)
            {
                throw new ElementNotFoundException("CourseNotFoundByName", name);
            }

            return course;
        }
        
        public async Task<bool> ContainsByCode(Space space, string code)
        {
            AssertHelper.NotNull(space, nameof(space));
            
            string normalizedCode = StringHelper.Normalize(code);
            return await _dbContext.Set<Course>()
                .Where(c => c.NormalizedCode == normalizedCode)
                .Where(c => c.SpaceId == space.Id)
                .AnyAsync();
        }
        
        public async Task<bool> ContainsByName(Space space, string name)
        {
            AssertHelper.NotNull(space, nameof(space));
            
            string normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Set<Course>()
                .Where(c => c.NormalizedName == normalizedName)
                .Where(c => c.SpaceId == space.Id)
                .AnyAsync();
        }
        
        public async Task<ActionResultModel<Course>> AddCourseAsync(Space space, CourseAddModel model, User user)
        {
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));
            
            var members = model.MemberIds
                .Select(async memberId => await _memberService.GetByIdAsync(memberId))
                .Select(t => t.Result)
                .ToList();
            
            var specialities = await _dbContext.Set<Speciality>()
                .Where(s => model.SpecialityIds.Contains(s.Id))
                .ToListAsync();
            
            
            string normalizedCode = StringHelper.Normalize(model.Code);
            string normalizedName = StringHelper.Normalize(model.Name);

            if (await ContainsByCode(space, model.Code))
            {
                throw new UsedValueException("CourseCodeUsed", model.Code);
            }

            if (await ContainsByName(space, model.Name))
            {
                throw new UsedValueException("CourseNameUsed", model.Name);
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

            
            var courseSpecialities = await _CreateCourseSpecialitiesAsync(course, specialities);
            await _dbContext.AddRangeAsync(courseSpecialities);

            var courseTeachers = await _CreateCourseTeachersCourseAsync(course, members);
            await _dbContext.AddRangeAsync(courseTeachers);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New course");

            var publisherIds = new List<string> {
                publisher.Id, 
                space.PublisherId
            };
            publisherIds.AddRange(members.Select(m => m.PublisherId));
            publisherIds.AddRange(members.Select(m => m.User!.PublisherId));
            publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_ADD", course);
            return new ActionResultModel<Course>(course, @event);
        }

        
        public async Task<Event> ChangeCourseCodeAsync(Course course, string code, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));

            if (await ContainsByCode(course.Space!, code))
            {
                throw new UsedValueException("CourseCodeUsed", code);
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
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));

            if (await ContainsByName(course.Space!, name))
            {
                throw new UsedValueException("CourseNameUsed", name);
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
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
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
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));

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

        
        public async Task<CourseSpeciality> GetCourseSpecialityAsync(ulong courseSpecialityId)
        {
            var courseSpeciality = await _dbContext.Set<CourseSpeciality>()
                .Include(cs => cs.Course)
                .Include(cs => cs.Speciality)
                .Where(cs => cs.Id == courseSpecialityId)
                .FirstOrDefaultAsync();

            if (courseSpeciality == null)
            {
                throw new ElementNotFoundException("CourseSpecialityNotFoundById", courseSpecialityId);
            }


            return courseSpeciality;
        }


        public async Task<bool> CourseSpecialityExists(Course course, Speciality speciality)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(speciality, nameof(speciality));

            return await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseId == course.Id)
                .Where(cs => cs.SpecialityId == speciality.Id)
                .Where(cs => cs.DeletedAt == null)
                .AnyAsync();
        }

        public async Task<ActionResultModel<CourseSpeciality>> AddCourseSpecialityAsync(Course course, 
            Speciality speciality, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(user, nameof(user));

            CourseSpeciality courseSpeciality = await _CreateCourseSpecialityAsync(course, speciality);
            await _dbContext.AddAsync(courseSpeciality);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {course.Space!.PublisherId, course.PublisherId, speciality.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITY_ADD", courseSpeciality);

            return new ActionResultModel<CourseSpeciality>(courseSpeciality, @event);
        }
        
        
        public async Task<ActionResultModel<ICollection<CourseSpeciality>>> AddCourseSpecialitiesAsync(Course course, 
            ICollection<Speciality> specialities, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(user, nameof(user));

            var courseSpecialities = await _CreateCourseSpecialitiesAsync(course, specialities);
            await _dbContext.AddRangeAsync(courseSpecialities);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {course.Space!.PublisherId, course.PublisherId};
            publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITIES_ADD", courseSpecialities);

            return new ActionResultModel<ICollection<CourseSpeciality>>(courseSpecialities, @event);
        }

        public async Task<List<CourseSpeciality>> _CreateCourseSpecialitiesAsync(Course course, 
            ICollection<Speciality> specialities)
        {
            var courseSpecialities = new List<CourseSpeciality>();
            foreach (var speciality in specialities)
            {
                if (!await CourseSpecialityExists(course, speciality))
                {
                    var courseSpeciality = await _CreateCourseSpecialityAsync(course, speciality);
                    courseSpecialities.Add(courseSpeciality);
                }
            }

            return courseSpecialities;
        }

        public async Task<CourseSpeciality> _CreateCourseSpecialityAsync(Course course, Speciality speciality)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(course.Space, nameof(course.Space));

            if (course.SpaceId != speciality.SpaceId)
            {
                throw new IncompatibleEntityException(course, speciality);
            }

            if (await CourseSpecialityExists(course, speciality))
            {
                throw new IllegalOperationException("CourseSpecialityAlreadyExists");
            }


            CourseSpeciality courseSpeciality = new()
            {
                Course = course,
                Speciality = speciality
            };
            return courseSpeciality;
        }

        public async Task<Event> DeleteCourseSpecialityAsync(CourseSpeciality courseSpeciality, User user)
        {
            AssertHelper.NotNull(courseSpeciality, nameof(courseSpeciality));
            AssertHelper.NotNull(user, nameof(user));
            var course = await _dbContext.Set<Course>().FindAsync(courseSpeciality.CourseId);
            var speciality = await _dbContext.Set<Speciality>().FindAsync(courseSpeciality.SpecialityId);
            var space = await _dbContext.Set<Space>().FindAsync(course!.SpaceId);
            
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(space, nameof(space));

            courseSpeciality.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(courseSpeciality);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {space!.PublisherId, speciality!.PublisherId, course.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITY_DELETE", courseSpeciality);
        }

       

        public async Task<bool> CourseTeacherExistsAsync(Course course, Member member)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(member, nameof(member));

            return await _dbContext.Set<CourseTeacher>()
                .Where(ct => ct.CourseId == course.Id)
                .Where(ct => ct.MemberId == member.Id)
                .Where(ct => ct.DeletedAt == null)
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


        public async Task<List<CourseTeacher>> _CreateCourseTeachersCourseAsync(Course course, List<Member> members)
        {
            var courseTeachers = new List<CourseTeacher>();
            foreach (var member in members)
            {
                if (!await CourseTeacherExistsAsync(course, member))
                {
                    var courseTeacher = await _CreateCourseTeacherAsync(course, member);
                    courseTeachers.Add(courseTeacher);
                }
            }

            return courseTeachers;
        }

        public async Task<CourseTeacher> _CreateCourseTeacherAsync(Course course, Member member)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(member, nameof(member));

            if (!member.IsTeacher)
            {
                throw new IllegalStateException("MemberIsNotTeacher");
            }

            CourseTeacher courseTeacher = new()
            {
                Course = course,
                Member = member
            };

            return courseTeacher;
        }
        
        public async Task<CourseTeacher> _CreateCourseTeacherAsync(Course course, ulong memberId)
        {
            AssertHelper.NotNull(course, nameof(course));

            var member = await _memberService.GetByIdAsync(memberId);
            return await _CreateCourseTeacherAsync(course, member);
        }
        
        public async Task DeleteCourseTeacherAsync(CourseTeacher courseTeacher)
        {
            AssertHelper.NotNull(courseTeacher, nameof(courseTeacher));
            _dbContext.Remove(courseTeacher);
            await _dbContext.SaveChangesAsync();
        }

        
        
        public async Task<Event> DeleteAsync(Course course, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(user, nameof(user));

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
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(user, nameof(user));

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