using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamBook.Services
{
    public class CourseSessionService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<CourseSessionService> _logger;

        public CourseSessionService(DbContext dbContext, ILogger<CourseSessionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task<CourseSession> AddCourseSessionAsync(Classroom classroom,
            CourseSessionAddModel model)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            
            Asserts.NotNull(model, nameof(model));

            var course = await _dbContext.Set<Course>().FindAsync(model.CourseId);
            var courseTeacher = await _dbContext.Set<CourseTeacher>().FindAsync(model.CourseTeacherId);
            var courseHour = await _dbContext.Set<CourseHour>().FindAsync(model.CourseHourId);


            CourseSession courseSession = new()
            {
                Classroom = classroom,
                Course = course,
                CourseTeacher = courseTeacher,
                CourseHour = courseHour,
                ExpectedStartDateTime = model.ExpectedStartDateTime,
                ExpectedEndDateTime = model.ExpectedEndDateTime,
                Description = model.Description
            };
            await _dbContext.AddAsync(courseSession);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New course session");
            return courseSession;
        }


        public async Task ChangeDateAsync(CourseSession courseSession, CourseSessionDateTimeModel model)
        {
            Asserts.NotNull(courseSession, nameof(courseSession));
            Asserts.NotNull(model, nameof(model));

            courseSession.ExpectedEndDateTime = model.ExpectedEndDateTime;
            courseSession.ExpectedStartDateTime = model.ExpectedStartDateTime;

            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ChangeDescription(CourseSession courseSession, string description)
        {
            Asserts.NotNull(courseSession, nameof(courseSession));

            courseSession.Description = description;

            _dbContext.Update(courseSession);
            await _dbContext.SaveChangesAsync();
        }


        public async Task DeleteAsync(CourseSession courseSession)
        {
            Asserts.NotNull(courseSession, nameof(courseSession));

            _dbContext.Remove(courseSession);
            await _dbContext.SaveChangesAsync();
        }
    }
}