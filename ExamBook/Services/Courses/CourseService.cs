using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
    public class CourseService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly SubjectService _subjectService;
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
        public async Task<Course> GetAsync(ulong id)
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


        public async Task<Course> GetByNameAsync(Space space, string name)
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
                throw new ElementNotFoundException("CourseNotFoundByName", space, name);
            }

            return course;
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
        
        public async Task<ActionResultModel<Course>> AddCourseAsync(Space space, CourseAddModel model, Member adminMember)
        {
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(adminMember, nameof(adminMember));
            
            string normalizedName = StringHelper.Normalize(model.Name);

            if (await ContainsByName(space, model.Name))
            {
                throw new UsedValueException("CourseNameUsed", space, model.Name);
            }

            var publisher = _publisherService.Create("COURSE_PUBLISHER");
            var subject = _subjectService.Create("COURSE_SUBJECT");
            Course course = new ()
            {
                Name = model.Name,
                NormalizedName = normalizedName,
                Description = model.Description,
                Space = space,
                PublisherId = publisher.Id,
                Publisher = publisher,
                Subject = subject,
                SubjectId = subject.Id
            };
            await _dbContext.AddAsync(course);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAsync(publisher);
            await _subjectService.SaveAsync(subject);
            
            _logger.LogInformation("New course");

            var publisherIds = new[] { publisher.Id, space.PublisherId };
            var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
            var data = new {CourseId = course.Id};
            var @event = await _eventService.EmitAsync(publisherIds, actorIds,subject.Id, "COURSE_ADD", data);
            return new ActionResultModel<Course>(course, @event);
        }

        
        
        

        public async Task<Event> ChangeCourseNameAsync(Course course, string name, Member adminMember)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(adminMember, nameof(adminMember));

            if (await ContainsByName(course.Space!, name))
            {
                throw new UsedValueException("CourseNameUsed", course.Space!, name);
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
            var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
            return await _eventService.EmitAsync(publisherIds, actorIds,course.SubjectId, "COURSE_CHANGE_NAME", eventData);
        }




        public async Task<Event> ChangeCourseDescriptionAsync(Course course, string description, Member adminMember)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));

            var eventData = new ChangeValueData<string>(course.Description, description);

            course.Description = description;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {course.PublisherId, course.Space!.PublisherId};
            var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
            return await _eventService.EmitAsync(publisherIds, actorIds, course.SubjectId, "COURSE_CHANGE_DESCRIPTION",
                eventData);
        }




        public async Task<Event> DeleteAsync(Course course, Member adminMember)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(adminMember, nameof(adminMember));

            course.Name = "";
            course.NormalizedName = "";
            course.Description = "";
            course.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(course);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { course.Space!.PublisherId, course.PublisherId };
            var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
            return await _eventService.EmitAsync(publisherIds,  actorIds, course.SubjectId, "COURSE_DELETE", course);
        }

        public async Task DestroyAsync(Course course, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(user, nameof(user));

            var courseClassrooms = await _dbContext.Set<CourseClassroom>()
                .Where(cc => cc.CourseId == course.Id)
                .ToListAsync();
            
            var courseSpecialities = await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseClassroom!.CourseId == course.Id)
                .ToListAsync();
            
            var courseTeachers = await _dbContext.Set<CourseTeacher>()
                .Where(ct => ct.CourseClassroom!.CourseId == course.Id)
                .ToListAsync();
            
            _dbContext.RemoveRange(courseClassrooms);
            _dbContext.RemoveRange(courseSpecialities);
            _dbContext.RemoveRange(courseTeachers);
            _dbContext.Remove(course);
            await _dbContext.SaveChangesAsync();
        }

    }
}