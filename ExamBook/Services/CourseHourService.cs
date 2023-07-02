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
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class CourseHourService
    {
        private readonly DbContext _dbContext;
        private readonly CourseTeacherService _courseTeacherService;
        private readonly RoomService _roomService;
        private readonly ILogger<CourseHourService> _logger;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;

        public CourseHourService(DbContext dbContext, 
            ILogger<CourseHourService> logger, 
            PublisherService publisherService,
            EventService eventService, RoomService roomService,
            CourseTeacherService courseTeacherService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
            _roomService = roomService;
            _courseTeacherService = courseTeacherService;
        }


        public async Task<ActionResultModel<CourseHour>> AddAsync(Course course, CourseHourAddModel model, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));

            var room = await _roomService.GetRoomAsync(model.RoomId);
            var courseTeacher = await _courseTeacherService.GetAsync(model.CourseTeacherId);

            var publisher = await _publisherService.AddAsync();
            CourseHour courseHour = new()
            {
                Course = course,
                Space = course.Space,
                CourseTeacher = courseTeacher,
                Room = room,
                DayOfWeek = model.DayOfWeek,
                StartHour = model.StartHour,
                EndHour = model.EndHour,
                PublisherId = publisher.Id
            };

            await _dbContext.AddAsync(courseHour);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string>
            {
                courseTeacher.Member!.PublisherId,
                course.Space!.PublisherId, 
                course.PublisherId, 
                publisher.Id
            };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_ADD", courseHour);
            _logger.LogInformation("New course hour");
            return new ActionResultModel<CourseHour>(courseHour, @event);
        }

        public async Task<Event> ChangeHourAsync(CourseHour courseHour, CourseHourHourModel model, User user)
        {
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(courseHour, nameof(courseHour));
            AssertHelper.NotNull(courseHour.Course, nameof(courseHour.Course));
            AssertHelper.NotNull(courseHour.Course.Space, nameof(courseHour.Course.Space));
            AssertHelper.NotNull(model, nameof(model));
            var course = courseHour.Course;

            var eventData = new ChangeValueData<CourseHourHourModel>(new CourseHourHourModel(courseHour), model);

            courseHour.StartHour = model.StartHour;
            courseHour.EndHour = model.EndHour;
            courseHour.DayOfWeek = model.DayOfWeek;
            _dbContext.Update(courseHour);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {course.Space!.PublisherId, course.PublisherId, courseHour.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_CHANGE", eventData);
        }

        
        
        public async Task<Event> ChangeTeacherAsync(CourseHour courseHour, CourseTeacher courseTeacher, User user)
        {
            AssertHelper.NotNull(courseHour, nameof(courseHour));
            AssertHelper.NotNull(courseTeacher, nameof(courseTeacher));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(courseHour.Course, nameof(courseHour.Course));
            AssertHelper.NotNull(courseHour.Course.Space, nameof(courseHour.Course.Space));
            var course = courseHour.Course;

            if (courseTeacher.CourseId != course.Id)
            {
                throw new IncompatibleEntityException(courseHour, courseTeacher);
            }

            var eventData = new ChangeValueData<ulong>(courseHour.CourseTeacher!.Id, courseTeacher.Id);
            courseHour.CourseTeacher = courseTeacher;
            _dbContext.Update(courseHour);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                course.Space!.PublisherId, 
                course.PublisherId, 
                courseHour.PublisherId,
                courseHour.CourseTeacher!.Member!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_CHANGE_TEACHER", eventData);
        }

        public async Task<Event> ChangeRoomAsync(CourseHour courseHour, Room room, User user)
        {
            AssertHelper.NotNull(courseHour, nameof(courseHour));
            AssertHelper.NotNull(room, nameof(room));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(courseHour.Course.Space, nameof(courseHour.Course.Space));
            AssertHelper.NotNull(courseHour.CourseTeacher!.Member, nameof(courseHour.CourseTeacher.Member));
            var course = courseHour.Course;

            if (room.SpaceId != courseHour.Course.SpaceId)
            {
                throw new IncompatibleEntityException(courseHour, room);
            }

            var eventData = new ChangeValueData<ulong>(courseHour.Room!.Id, room.Id);
            courseHour.Room = room;
            _dbContext.Update(courseHour);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                course.Space!.PublisherId, 
                course.PublisherId, 
                courseHour.PublisherId,
                courseHour.CourseTeacher!.Member!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_HOUR_CHANGE_ROOM", eventData);
        }


        public async Task<Event> DeleteAsync(CourseHour courseHour, bool courseSession, User user)
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


    }
}