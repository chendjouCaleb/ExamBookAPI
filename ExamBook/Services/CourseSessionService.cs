using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
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
    public class CourseSessionService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<CourseSessionService> _logger;

        public CourseSessionService(DbContext dbContext, 
            ILogger<CourseSessionService> logger,
            PublisherService publisherService, 
            EventService eventService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
        }

        public async Task<CourseSession> GetAsync(ulong id)
        {
            var courseSession = await _dbContext.Set<CourseSession>()
                .Include(cs => cs.Course!.Space)
                .Include(cs => cs.CourseTeacher!.Member)
                .Where(cs => cs.Id == id)
                .FirstOrDefaultAsync();

            if (courseSession == null)
            {
                throw new ElementNotFoundException("CourseSessionNotFound");
            }

            return courseSession;
        }


        public async Task<ActionResultModel<CourseSession>> AddAsync(Space space, CourseSessionAddModel model, User user)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(model, nameof(model));

            var course = await _dbContext.Set<Course>().FindAsync(model.CourseId);
            var courseTeacher = await _dbContext.Set<CourseTeacher>()
                .Include(ct => ct.Member)
                .Where(ct => ct.Id == model.CourseTeacherId)
                .FirstAsync();
            var courseHour = await _dbContext.Set<CourseHour>().FindAsync(model.CourseHourId);

            var publisher = await _publisherService.AddAsync();
            CourseSession courseSession = new()
            {
                Space = space,
                Course = course,
                CourseTeacher = courseTeacher,
                CourseHour = courseHour,
                ExpectedStartDateTime = model.ExpectedStartDateTime,
                ExpectedEndDateTime = model.ExpectedEndDateTime,
                Description = model.Description,
                PublisherId = publisher.Id
            };
            await _dbContext.AddAsync(courseSession);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New course session");

            var publisherIds = new List<string>
            {
                publisher.Id,
                space.PublisherId,
                course!.PublisherId,
                courseTeacher.Member!.PublisherId
            };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SESSION_ADD", courseSession);
            return new ActionResultModel<CourseSession>(courseSession, @event);
        }

        
        public async Task<Event> ReportAsync(CourseSession courseSession, CourseSessionReportModel model, User user)
        {
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(courseSession, nameof(courseSession));
            Asserts.NotNull(courseSession.Course!.Space, nameof(courseSession.Course.Space));
            Asserts.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            Asserts.NotNull(model, nameof(model));
            var course = courseSession.Course;
            
            courseSession.Report = model.Report;
            courseSession.StartDateTime = model.StartDateTime;
            courseSession.EndDateTime = model.EndDateTime;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                course.Space!.PublisherId, 
                course.PublisherId, 
                courseSession.CourseTeacher.PublisherId,
                courseSession.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_REPORT", model);
        }

        
        public async Task<Event> ChangeHourAsync(CourseSession courseSession, CourseSessionDateModel model, User user)
        {
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(courseSession, nameof(courseSession));
            Asserts.NotNull(courseSession.Course!.Space, nameof(courseSession.Course.Space));
            Asserts.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            Asserts.NotNull(model, nameof(model));
            var course = courseSession.Course;

            var eventData = new ChangeValueData<CourseSessionDateModel>(new CourseSessionDateModel(courseSession), model);

            courseSession.ExpectedEndDateTime = model.ExpectedEndDateTime;
            courseSession.ExpectedStartDateTime = model.ExpectedStartDateTime;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                course.Space!.PublisherId, 
                course.PublisherId, 
                courseSession.CourseTeacher.PublisherId,
                courseSession.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_CHANGE_DATE", eventData);
        }

        
        
        public async Task<Event> ChangeTeacherAsync(CourseSession courseSession, CourseTeacher courseTeacher, User user)
        {
            Asserts.NotNull(courseSession, nameof(courseSession));
            Asserts.NotNull(courseTeacher, nameof(courseTeacher));
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(courseSession.Course, nameof(courseSession.Course));
            Asserts.NotNull(courseSession.Course!.Space, nameof(courseSession.Course.Space));
            var course = courseSession.Course;

            if (courseTeacher.CourseId != course.Id)
            {
                throw new IncompatibleEntityException(courseSession, courseTeacher);
            }

            var eventData = new ChangeValueData<ulong>(courseSession.CourseTeacher!.Id, courseTeacher.Id);
            courseSession.CourseTeacher = courseTeacher;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                courseTeacher.Member!.PublisherId,
                course.Space!.PublisherId,
                course.PublisherId, 
                courseSession.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SESSION_CHANGE_TEACHER", eventData);
        }

        public async Task<Event> ChangeRoomAsync(CourseSession courseSession, Room room, User user)
        {
            Asserts.NotNull(room, nameof(room));
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(courseSession.Course!.Space, nameof(courseSession.Course.Space));
            Asserts.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            var course = courseSession.Course;

            if (room.SpaceId != courseSession.Course.SpaceId)
            {
                throw new IncompatibleEntityException(courseSession, room);
            }

            var eventData = new ChangeValueData<ulong>(courseSession.Room!.Id, room.Id);
            courseSession.Room = room;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                course.Space!.PublisherId, 
                course.PublisherId, 
                courseSession.CourseTeacher.PublisherId,
                courseSession.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SESSION_CHANGE_ROOM", eventData);
        }

        public async Task<Event> ChangeDescription(CourseSession courseSession, string description, User user)
        {
            Asserts.NotNull(courseSession, nameof(courseSession));
            Asserts.NotNull(courseSession, nameof(courseSession));
            Asserts.NotNull(courseSession.Course!.Space, nameof(courseSession.Course.Space));
            Asserts.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            var course = courseSession.Course!;

            var eventData = new ChangeValueData<string>(courseSession.Description, description);
            courseSession.Description = description;

            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                course.Space!.PublisherId, 
                course.PublisherId, 
                courseSession.CourseTeacher.PublisherId,
                courseSession.PublisherId
            };
            string eventName = "COURSE_SESSION_CHANGE_DESCRIPTION";
            return await _eventService.EmitAsync(publisherIds, user.ActorId, eventName, eventData);
        }


        public async Task<Event> DeleteAsync(CourseSession courseSession, User user)
        {
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(courseSession, nameof(courseSession));
            Asserts.NotNull(courseSession.Course!.Space, nameof(courseSession.Course.Space));
            Asserts.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            var course = courseSession.Course!;

            courseSession.Description = "";
            courseSession.Report = "";
            courseSession.EndDateTime = DateTime.MinValue;
            courseSession.StartDateTime = DateTime.MinValue;
            courseSession.ExpectedEndDateTime = DateTime.MinValue;
            courseSession.ExpectedStartDateTime = DateTime.MinValue;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                courseSession.CourseTeacher.PublisherId,
                course.Space!.PublisherId,
                course.PublisherId, 
                courseSession.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SESSION_DELETE", courseSession);
        }
    }
}