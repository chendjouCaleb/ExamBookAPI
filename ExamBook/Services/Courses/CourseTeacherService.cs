using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

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
                .Include(ct => ct.Member)
                .Where(ct => ct.Id == id)
                .FirstOrDefaultAsync();

            if (courseTeacher == null)
            {
                throw new ElementNotFoundException("CourseTeacherNotFoundById");
            }

            return courseTeacher;
        }


        public async Task<bool> ContainsAsync(CourseClassroom courseClassroom, Member member)
        {
            return await _dbContext.Set<CourseTeacher>()
                .Where(ct => ct.CourseClassroomId == courseClassroom.Id && ct.MemberId == member.Id)
                .Where(ct => ct.DeletedAt == null)
                .AnyAsync();
        }


        public async Task<ActionResultModel<CourseTeacher>> AddAsync(Course course, Member member, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(member, nameof(member));
            AssertHelper.NotNull(user, nameof(user));
            var space = await _dbContext.Set<Space>().FindAsync(course.SpaceId);

            AssertHelper.NotNull(member, nameof(member));
            AssertHelper.NotNull(space, nameof(space));

            CourseTeacher courseTeacher = await _CreateCourseTeacherAsync(course, member);
            await _dbContext.AddAsync(courseTeacher);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {space!.PublisherId, course.PublisherId, member.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHER_ADD", courseTeacher);
            _logger.LogInformation("course teacher add");
            return new ActionResultModel<CourseTeacher>(courseTeacher, @event);
        }


        public async Task<ActionResultModel<ICollection<CourseTeacher>>> AddCourseTeachersAsync(Course course,
            ICollection<Member> members, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(members, nameof(members));
            AssertHelper.NotNull(user, nameof(user));

            var courseTeachers = await _CreateCourseTeachersAsync(course, members);
            await _dbContext.AddRangeAsync(courseTeachers);
            await _dbContext.SaveChangesAsync();

            var publisherIds = ImmutableList
                .Create(course.Space!.PublisherId, course.PublisherId)
                .AddRange(members.Select(s => s.PublisherId));
            var @event =
                await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHERS_ADD", courseTeachers);

            return new ActionResultModel<ICollection<CourseTeacher>>(courseTeachers, @event);
        }


        public async Task<CourseTeacher> _CreateCourseTeacherAsync(CourseClassroom courseClassroom, Member member)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(member, nameof(member));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));

            if (courseClassroom.Course.SpaceId != member.SpaceId)
            {
                throw new IncompatibleEntityException(courseClassroom, member);
            }

            if (await ContainsAsync(courseClassroom, member))
            {
                throw new IllegalOperationException("CourseTeacherAlreadyExists", courseClassroom, member);
            }


            CourseTeacher courseTeacher = new()
            {
                CourseClassroom = courseClassroom,
                Member = member
            };
            return courseTeacher;
        }

        public async Task<List<CourseTeacher>> _CreateCourseTeachersAsync(Course course, ICollection<Member> members)
        {
            var courseSpecialities = new List<CourseTeacher>();
            foreach (var member in members)
            {
                if (await ContainsAsync(course, member)) continue;
                var courseMember = await _CreateCourseTeacherAsync(course, member);
                courseSpecialities.Add(courseMember);
            }

            return courseSpecialities;
        }

        public async Task<Event> SetAsPrincipalAsync(CourseTeacher courseTeacher, User user)
        {
            AssertHelper.NotNull(courseTeacher, nameof(courseTeacher));
            AssertHelper.NotNull(user, nameof(user));

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

            var publisherIds = new List<string> {space!.PublisherId, course.PublisherId, member!.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHER_SET_PRINCIPAL", new { });
        }


        public async Task<Event> UnSetAsPrincipalAsync(CourseTeacher courseTeacher, User user)
        {
            AssertHelper.NotNull(courseTeacher, nameof(courseTeacher));
            AssertHelper.NotNull(user, nameof(user));

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

            var publisherIds = new List<string> {space!.PublisherId, course.PublisherId, member!.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_TEACHER_UNSET_PRINCIPAL", new { });
        }


        public async Task<Event> DeleteAsync(CourseTeacher courseTeacher, Member member)
        {
            AssertHelper.NotNull(courseTeacher, nameof(courseTeacher));
            AssertHelper.NotNull(courseTeacher.Member, nameof(courseTeacher.Member));
            AssertHelper.NotNull(courseTeacher.CourseClassroom, nameof(courseTeacher.CourseClassroom));
            AssertHelper.NotNull(courseTeacher.CourseClassroom!.Course, nameof(courseTeacher.CourseClassroom.Course));
            AssertHelper.NotNull(courseTeacher.CourseClassroom.Course.Space, nameof(courseTeacher.CourseClassroom.Course.Space));
            AssertHelper.NotNull(courseTeacher.Member, nameof(courseTeacher.Member));
            AssertHelper.NotNull(member, nameof(member));

            courseTeacher.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(courseTeacher);
            await _dbContext.SaveChangesAsync();

            
            var publisherIds = new List<string> {
                courseTeacher.PublisherId, 
                courseTeacher.CourseClassroom.PublisherId,
                courseTeacher.CourseClassroom.Course.PublisherId,
                courseTeacher.CourseClassroom.Course.Space!.PublisherId,
                courseTeacher.Member!.PublisherId
            };
            var actorIds = new[] {member.ActorId, member.User!.ActorId};
            var data = new {CourseTeacherId = courseTeacher.Id};
            return await _eventService.EmitAsync(publisherIds, actorIds, courseTeacher.SubjectId, "COURSE_TEACHER_DELETE", data);
        }
    }
}