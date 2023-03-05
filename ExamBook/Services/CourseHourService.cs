using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamBook.Services
{
    public class CourseHourService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<CourseHourService> _logger;

        public CourseHourService(DbContext dbContext, ILogger<CourseHourService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task<CourseHour> AddCourseAsync(Classroom classroom, CourseHourAddModel model)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNull(model, nameof(model));

            var course = await _dbContext.Set<Course>().FindAsync();
            var room = await _dbContext.Set<Room>().FindAsync(model.RoomId);
            var courseTeacher = await _dbContext.Set<CourseTeacher>().FindAsync(model.RoomId);

            if (course == null)
            {
                
            }

            if (room == null)
            {
                
            }

            if (courseTeacher == null)
            {
                
            }

            CourseHour courseHour = new CourseHour
            {
                Classroom = classroom,
                Course = course,
                CourseTeacher = courseTeacher,
                Room = room,
                DayOfWeek = model.DayOfWeek,
                StartHour = model.StartHour,
                EndHour = model.EndHour
            };

            await _dbContext.AddAsync(courseHour);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New course hour");
            return courseHour;
        }

        public async Task ChangeHourAsync(CourseHour courseHour, CourseHourChangeHourModel model)
        {
            Asserts.NotNull(courseHour, nameof(courseHour));
            Asserts.NotNull(model, nameof(model));

            courseHour.StartHour = model.StartHour;
            courseHour.EndHour = model.EndHour;
            courseHour.DayOfWeek = model.DayOfWeek;
            _dbContext.Update(courseHour);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ChangeTeacherAsync(CourseHour courseHour, CourseTeacher courseTeacher)
        {
            Asserts.NotNull(courseHour, nameof(courseHour));
            Asserts.NotNull(courseTeacher, nameof(courseTeacher));

            courseHour.CourseTeacher = courseTeacher;
            _dbContext.Update(courseHour);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ChangeRoomAsync(CourseHour courseHour, Room room)
        {
            Asserts.NotNull(courseHour, nameof(courseHour));
            Asserts.NotNull(courseHour.Course, nameof(courseHour.Course));
            Asserts.NotNull(room, nameof(room));

            if (courseHour.Course!.SpaceId != room.SpaceId)
            {
                
            }
            
            courseHour.Room = room;
            
            _dbContext.Update(courseHour);
            await _dbContext.SaveChangesAsync();
        }


        public async Task Delete(CourseHour courseHour, bool courseSession)
        {
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
        }


    }
}