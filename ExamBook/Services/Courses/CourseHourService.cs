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
    public class CourseHourService
    {
        private readonly DbContext _dbContext;
        private readonly CourseTeacherService _courseTeacherService;
        private readonly RoomService _roomService;
        private readonly ILogger<CourseHourService> _logger;
        private readonly PublisherService _publisherService;
        private readonly SubjectService _subjectService;
        private readonly EventService _eventService;

        public CourseHourService(DbContext dbContext, 
            ILogger<CourseHourService> logger, 
            PublisherService publisherService,
            EventService eventService, RoomService roomService,
            CourseTeacherService courseTeacherService, SubjectService subjectService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
            _roomService = roomService;
            _courseTeacherService = courseTeacherService;
            _subjectService = subjectService;
        }


        public async Task<ActionResultModel<CourseHour>> AddAsync(CourseClassroom courseClassroom, 
        CourseHourAddModel model, Member adminMember)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(adminMember, nameof(adminMember));

            var room = await _roomService.GetRoomAsync(model.RoomId);
            var courseTeacher = await _courseTeacherService.GetAsync(model.CourseTeacherId);

            var publisher = _publisherService.Create("COURSE_HOUR_PUBLISHER");
            var subject = _subjectService.Create("COURSE_HOUR_SUBJECT");
            CourseHour courseHour = new()
            {
                CourseClassroom = courseClassroom,
                Space = courseClassroom.Course.Space,
                CourseTeacher = courseTeacher,
                Room = room,
                DayOfWeek = model.DayOfWeek,
                StartHour = model.StartHour,
                EndHour = model.EndHour,
                PublisherId = publisher.Id,
                Publisher = publisher,
                Subject = subject,
                SubjectId = subject.Id
            };
            
            await _dbContext.AddAsync(courseHour);
            await _dbContext.SaveChangesAsync();
            await _subjectService.SaveAsync(subject);
            await _publisherService.SaveAsync(publisher);
            
            var publisherIds = new List<string>
            {
                
                courseTeacher.Member!.PublisherId,
                courseTeacher.PublisherId,
                courseClassroom.Course.Space!.PublisherId, 
                courseClassroom.Course.PublisherId,
                room.PublisherId,
                publisher.Id
            };
            var actorIds = new[] {adminMember.ActorId, adminMember.User!.ActorId};
            var data = new {CourseHourId = courseHour.Id};
            var @event = await _eventService.EmitAsync(publisherIds, actorIds, subject.Id, "COURSE_HOUR_ADD", data);
            _logger.LogInformation("New course hour");
            return new ActionResultModel<CourseHour>(courseHour, @event);
        }

        public async Task<Event> ChangeHourAsync(CourseHour courseHour, CourseHourHourModel model, Member memberId)
        {
            AssertHelper.NotNull(memberId, nameof(memberId));
            AssertNotNull(courseHour);
            AssertHelper.NotNull(model, nameof(model));

            var eventData = new ChangeValueData<CourseHourHourModel>(new CourseHourHourModel(courseHour), model);

            courseHour.StartHour = model.StartHour;
            courseHour.EndHour = model.EndHour;
            courseHour.DayOfWeek = model.DayOfWeek;
            _dbContext.Update(courseHour);
            await _dbContext.SaveChangesAsync();
            
            var actorIds = new[] {adminMember.ActorId, adminMember.User!.ActorId};
            var publisherIds = new List<string> {course.Space!.PublisherId, course.PublisherId, courseHour.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_CHANGE", eventData);
        }

        
        
        


        public async Task<Event> DeleteAsync(CourseHour courseHour, bool courseSession, Member member)
        {
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(courseHour, nameof(courseHour));
            AssertHelper.NotNull(courseHour.Course.Space, nameof(courseHour.Course.Space));
            AssertHelper.NotNull(courseHour.CourseTeacher!.Member, nameof(courseHour.CourseTeacher.Member));
            var course = courseHour.Course;
            
            var courseSessions = await  _dbContext.Set<CourseSession>()
                .Where(cs => cs.CourseHour == courseHour)
                .ToListAsync();
            
            if (courseSession)
            {
                _dbContext.RemoveRange(courseSessions);
            }
            else
            {
                foreach (var session in courseSessions)
                {
                    session.CourseHourId = null;
                    session.CourseHour = null;
                }
                _dbContext.UpdateRange(courseSessions);
            }
            
            _dbContext.Remove(courseHour);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                course.Space!.PublisherId, 
                course.PublisherId, 
                courseHour.PublisherId,
                courseHour.CourseTeacher.Member!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_DELETE", courseHour);
            
        }


        public static HashSet<string> GetPublisherIds(CourseHour courseHour)
        {
            var publisherIds = new HashSet<string>
            {
                courseHour.PublisherId,
                courseHour.CourseClassroom!.PublisherId,
                courseHour.CourseClassroom.Course.PublisherId,
                courseHour.CourseClassroom.Course.Space!.PublisherId
            };

            
            if (courseHour.CourseTeacherId != null)
            {
                publisherIds = publisherIds.Concat(new[]
                {
                    courseHour.CourseTeacher!.PublisherId,
                    courseHour.CourseTeacher!.Member!.PublisherId
                }).ToHashSet();
            }

            if (courseHour.RoomId != null)
            {
                publisherIds = publisherIds.Append(courseHour.Room!.PublisherId).ToHashSet();
            }

            return publisherIds;
        }
        
        public static void AssertNotNull(CourseHour courseHour)
        {
            AssertHelper.NotNull(courseHour, nameof(courseHour));
            AssertHelper.NotNull(courseHour.CourseClassroom, nameof(courseHour.CourseClassroom));
            AssertHelper.NotNull(courseHour.CourseClassroom!.Course, nameof(courseHour.CourseClassroom.Course));
            AssertHelper.NotNull(courseHour.CourseClassroom.Course.Space,
                nameof(courseHour.CourseClassroom.Course.Space));

            if (courseHour.CourseTeacherId != null)
            {
                AssertHelper.NotNull(courseHour.CourseTeacher, nameof(courseHour.CourseTeacher));
                AssertHelper.NotNull(courseHour.CourseTeacher!.Member, nameof(courseHour.CourseTeacher.Member));
            }

            if (courseHour.RoomId != null)
            {
                AssertHelper.NotNull(courseHour.Room, nameof(courseHour.Room));
            }
        }
    }
}