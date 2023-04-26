using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class CourseTeacherService
    {
        private readonly DbContext _dbContext;
        private readonly EventService _eventService;
        private readonly ILogger<CourseTeacherService> _logger;

        public CourseTeacherService(DbContext dbContext,
            EventService eventService, 
            ILogger<CourseTeacherService> logger)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _logger = logger;
        }

        public async Task<CourseTeacher> GetAsync(ulong id)
        {
            var courseTeacher = await _dbContext.Set<CourseTeacher>()
                .Where(ct => ct.Id == id)
                .FirstOrDefaultAsync();

            if (courseTeacher == null)
            {
                throw new ElementNotFoundException("CourseTeacherNotFoundById");
            }

            return courseTeacher;
        }


        public async Task<bool> ContainsAsync(Course course, Member member)
        {
            return await _dbContext.Set<CourseTeacher>()
                .Where(ct => ct.CourseId == course.Id && ct.MemberId == member.Id)
                .Where(ct => ct.DeletedAt == null)
                .AnyAsync();
        }


        public async Task<ActionResultModel<CourseTeacher>> AddAsync(Course course, CourseTeacherAddModel model, User user)
        {
            Asserts.NotNull(course, nameof(course));
            Asserts.NotNull(model, nameof(model));
            Asserts.NotNull(user, nameof(user));

            var member = await _dbContext.Set<Member>()
                .Where(m => m.Id == model.MemberId)
                .FirstOrDefaultAsync();
            var space = await _dbContext.Set<Space>().FindAsync(course.SpaceId);
            
            Asserts.NotNull(member, nameof(member));
            Asserts.NotNull(space, nameof(space));

            if (await ContainsAsync(course, member!))
            {
                throw new IllegalOperationException("CourseTeacherAlreadyExists");
            }

            CourseTeacher courseTeacher = new ()
            {
                Course = course,
                Member = member,
                IsPrincipal = model.IsPrincipal
            };
            await _dbContext.AddAsync(courseTeacher);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string>() { space!.PublisherId, course.PublisherId, member!.PublisherId };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHER_ADD", courseTeacher);
            _logger.LogInformation("course teacher add");
            return new ActionResultModel<CourseTeacher>(courseTeacher, @event);
        }


        public async Task<Event> SetAsPrincipalAsync(CourseTeacher courseTeacher, User user)
        {
            Asserts.NotNull(courseTeacher, nameof(courseTeacher));
            Asserts.NotNull(user, nameof(user));

            if (!courseTeacher.IsPrincipal)
            {
                throw new IllegalStateException("CourseTeacherIsAlreadyPrincipal");
            }
            
            var member = await _dbContext.Set<Member>().FindAsync(courseTeacher.MemberId);
            var course = await _dbContext.Set<Course>().FindAsync(courseTeacher);
            var space = await _dbContext.Set<Space>().FindAsync(course!.Space);
            
            courseTeacher.IsPrincipal = true;
            _dbContext.Update(courseTeacher);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { space!.PublisherId, course.PublisherId, member!.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHER_SET_PRINCIPAL", new { });
        }
        
        
        public async Task<Event> UnSetAsPrincipalAsync(CourseTeacher courseTeacher, User user)
        {
            Asserts.NotNull(courseTeacher, nameof(courseTeacher));
            Asserts.NotNull(user, nameof(user));

            if (courseTeacher.IsPrincipal)
            {
                throw new IllegalStateException("CourseTeacherIsPrincipal");
            }
            
            var member = await _dbContext.Set<Member>().FindAsync(courseTeacher.MemberId);
            var course = await _dbContext.Set<Course>().FindAsync(courseTeacher);
            var space = await _dbContext.Set<Space>().FindAsync(course!.Space);

            courseTeacher.IsPrincipal = false;
            _dbContext.Update(courseTeacher);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { space!.PublisherId, course.PublisherId, member!.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHER_UNSET_PRINCIPAL", new { });
        }


        public async Task<Event> DeleteAsync(CourseTeacher courseTeacher, User user)
        {
            Asserts.NotNull(courseTeacher, nameof(courseTeacher));
            Asserts.NotNull(user, nameof(user));

            var course = await _dbContext.Set<Course>()
                .Include(c => c.Space)
                .Where(c => c.Id == courseTeacher.CourseId)
                .FirstAsync();
            var member = await _dbContext.Set<Member>().Where(m => m.Id == courseTeacher.MemberId)
                .FirstAsync();
            var space = course.Space;
            
            courseTeacher.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(courseTeacher);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { space!.PublisherId, member.PublisherId, course.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHER_DELETE", courseTeacher);
        }
    }
}