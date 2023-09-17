using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
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
    public class CourseSessionService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly SubjectService _subjectService;
        private readonly EventService _eventService;
        private readonly CourseClassroomService _courseClassroomService;
        private readonly CourseTeacherService _courseTeacherService;
        private readonly ILogger<CourseSessionService> _logger;

        public CourseSessionService(DbContext dbContext, 
            ILogger<CourseSessionService> logger,
            PublisherService publisherService, 
            EventService eventService, SubjectService subjectService,
            CourseClassroomService courseClassroomService, CourseTeacher courseTeacher, 
            CourseTeacherService courseTeacherService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
            _subjectService = subjectService;
            _courseClassroomService = courseClassroomService;
            _courseTeacherService = courseTeacherService;
        }

        public async Task<CourseSession> GetAsync(ulong id)
        {
            var courseSession = await _dbContext.Set<CourseSession>()
                .Include(cs => cs.CourseClassroom!.Course.Space)
                .Include(cs => cs.CourseTeacher!.Member)
                .Where(cs => cs.Id == id)
                .FirstOrDefaultAsync();

            if (courseSession == null)
            {
                throw new ElementNotFoundException("CourseSessionNotFound", id);
            }

            return courseSession;
        }


        public async Task<ActionResultModel<CourseSession>> AddAsync(Space space, CourseSessionAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(model, nameof(model));

            var course = await _courseClassroomService.GetAsync(model.CourseClassroomId);
            var courseTeacher = await _courseTeacherService.GetAsync(model.CourseTeacherId);
            var courseHour = await _dbContext.Set<CourseHour>().FindAsync(model.CourseHourId);

            var publisher = await _publisherService.AddAsync();
            CourseSession courseSession = new()
            {
                Space = space,
                CourseClassroom = course,
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

        
        public async Task<Event> ReportAsync(CourseSession courseSession, CourseSessionReportModel model, Member adminMember)
        {
            AssertHelper.NotNull(adminMember, nameof(adminMember));
            AssertHelper.NotNull(courseSession, nameof(courseSession));
            AssertHelper.NotNull(courseSession.CourseClassroom!.Course.Space, nameof(courseSession.CourseClassroom.Course.Space));
            AssertHelper.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            AssertHelper.NotNull(model, nameof(model));
            var courseClassroom = courseSession.CourseClassroom;
            
            courseSession.Report = model.Report;
            courseSession.StartDateTime = model.StartDateTime;
            courseSession.EndDateTime = model.EndDateTime;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                courseClassroom.Course.Space!.PublisherId, 
                courseClassroom.PublisherId, 
                courseSession.CourseTeacher.PublisherId,
                courseSession.PublisherId
            };
            var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
            return await _eventService.EmitAsync(publisherIds, actorIds, courseSession.SubjectId, "COURSE_SESSION_REPORT", model);
        }

        
        public async Task<Event> ChangeHourAsync(CourseSession courseSession, CourseSessionDateModel model, Member adminMember)
        {
            AssertHelper.NotNull(adminMember, nameof(adminMember));
            AssertHelper.NotNull(courseSession, nameof(courseSession));
            AssertHelper.NotNull(courseSession.CourseClassroom!.Course!.Space, nameof(courseSession.CourseClassroom.Course.Space));
            AssertHelper.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            AssertHelper.NotNull(model, nameof(model));
            var courseClassroom = courseSession.CourseClassroom;

            var eventData = new ChangeValueData<CourseSessionDateModel>(new CourseSessionDateModel(courseSession), model);

            courseSession.ExpectedEndDateTime = model.ExpectedEndDateTime;
            courseSession.ExpectedStartDateTime = model.ExpectedStartDateTime;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                courseClassroom.Course.Space!.PublisherId, 
                courseClassroom.PublisherId, 
                courseSession.CourseTeacher.PublisherId,
                courseSession.PublisherId
            };
            
            var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
            return await _eventService.EmitAsync(publisherIds, actorIds, courseClassroom.SubjectId,
                "COURSE_SESSION_CHANGE_DATE", eventData);
        }

        
        
        public async Task<Event> ChangeTeacherAsync(CourseSession courseSession, CourseTeacher courseTeacher, Member adminMember)
        {
            AssertHelper.NotNull(courseSession, nameof(courseSession));
            AssertHelper.NotNull(courseTeacher, nameof(courseTeacher));
            AssertHelper.NotNull(adminMember, nameof(adminMember));
            AssertHelper.NotNull(courseSession.CourseClassroom!.Course, nameof(courseSession.CourseClassroom.Course));
            AssertHelper.NotNull(courseSession.CourseClassroom.Course!.Space, nameof(courseSession.CourseClassroom.Course.Space));
            var courseClassroom = courseSession.CourseClassroom;

            if (courseTeacher.CourseClassroomId != courseClassroom.Id)
            {
                throw new IncompatibleEntityException(courseSession, courseTeacher);
            }

            var eventData = new ChangeValueData<ulong>(courseSession.CourseTeacher!.Id, courseTeacher.Id);
            courseSession.CourseTeacher = courseTeacher;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();

            var publisherIds = GetPublisherIds(courseSession);
            
            var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
            return await _eventService.EmitAsync(publisherIds, actorIds, courseSession.SubjectId, "COURSE_SESSION_CHANGE_TEACHER", eventData);
        }

        public async Task<Event> ChangeRoomAsync(CourseSession courseSession, Room room, User user)
        {
            AssertNotNull(courseSession);

            if (room.SpaceId != courseSession.CourseClassroom.Course.SpaceId)
            {
                throw new IncompatibleEntityException(courseSession, room);
            }

            var eventData = new ChangeValueData<ulong>(courseSession.Room!.Id, room.Id);
            courseSession.Room = room;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();

            var publisherIds = GetPublisherIds(courseSession);
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SESSION_CHANGE_ROOM", eventData);
        }

        public async Task<Event> ChangeDescription(CourseSession courseSession, string description, User user)
        {
            AssertNotNull(courseSession);

            var eventData = new ChangeValueData<string>(courseSession.Description, description);
            courseSession.Description = description;

            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();

            var publisherIds = GetPublisherIds(courseSession);
            string eventName = "COURSE_SESSION_CHANGE_DESCRIPTION";
            return await _eventService.EmitAsync(publisherIds, user.ActorId, eventName, eventData);
        }


        public async Task<Event> DeleteAsync(CourseSession courseSession, User user)
        {
            AssertNotNull(courseSession);

            courseSession.Description = "";
            courseSession.Report = "";
            courseSession.EndDateTime = DateTime.MinValue;
            courseSession.StartDateTime = DateTime.MinValue;
            courseSession.ExpectedEndDateTime = DateTime.MinValue;
            courseSession.ExpectedStartDateTime = DateTime.MinValue;
            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();

            var publisherIds = GetPublisherIds(courseSession);
            
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SESSION_DELETE", courseSession);
        }
        
        
        private static void AssertNotNull(CourseSession courseSession)
        {
            AssertHelper.NotNull(courseSession, nameof(courseSession));
            AssertHelper.NotNull(courseSession.CourseClassroom, nameof(courseSession.CourseClassroom));
            AssertHelper.NotNull(courseSession.CourseClassroom!.Course, nameof(courseSession.CourseClassroom.Course));
            AssertHelper.NotNull(courseSession.CourseClassroom.Course.Space,
                nameof(courseSession.CourseClassroom.Course.Space));

            if (courseSession.CourseTeacherId != null)
            {
                AssertHelper.NotNull(courseSession.CourseTeacher, nameof(courseSession.CourseTeacher));
                AssertHelper.NotNull(courseSession.CourseTeacher!.Member, nameof(courseSession.CourseTeacher.Member));
            }

            if (courseSession.RoomId != null)
            {
                AssertHelper.NotNull(courseSession.Room, nameof(courseSession.Room));
            }
        }
        
        
        public static HashSet<string> GetPublisherIds(CourseSession courseSession)
        {
            var publisherIds = new HashSet<string>
            {
                courseSession.PublisherId,
                courseSession.CourseClassroom.PublisherId,
                courseSession.CourseClassroom.Course.PublisherId,
                courseSession.CourseClassroom.Course.Space!.PublisherId
            };

            
            if (courseSession.CourseTeacherId != null)
            {
                var courseTeacher = courseSession.CourseTeacher!;
                publisherIds = publisherIds.Concat(new[]
                {
                    courseTeacher.PublisherId, courseTeacher.Member!.PublisherId
                }).ToHashSet();
            }

            if (courseSession.RoomId != null)
            {
                publisherIds = publisherIds.Append(courseSession.Room!.PublisherId).ToHashSet();
            }
            
            if (courseSession.CourseHourId != null)
            {
                publisherIds = publisherIds.Append(courseSession.CourseHour!.PublisherId).ToHashSet();
            }

            return publisherIds;
        }
    }
}