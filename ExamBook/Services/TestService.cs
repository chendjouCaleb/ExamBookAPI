using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vx.Services;

namespace ExamBook.Services
{
    public class TestService
    {
        private readonly DbContext _dbContext;
        private readonly PaperService _paperService;
        private readonly RoomService _roomService;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<TestService> _logger;

        public TestService(DbContext dbContext, 
            PaperService paperService, 
            ILogger<TestService> logger, 
            EventService eventService, 
            PublisherService publisherService, RoomService roomService)
        {
            _dbContext = dbContext;
            _paperService = paperService;
            _logger = logger;
            _eventService = eventService;
            _publisherService = publisherService;
            _roomService = roomService;
        }

        public async Task<ActionResultModel<Test>> AddAsync(Space space, TestAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));
            var test = await CreateTest(space, model);
            var publisher = await _publisherService.AddAsync();
            test.PublisherId = publisher.Id;

            await _dbContext.AddAsync(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {space.PublisherId, publisher.Id, test.Room!.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }

        public async Task<ActionResultModel<Test>> Add(Course course, TestAddModel model, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));
            var space = course.Space!;
            var test = await CreateTest(space!, model);
            test.Course = course;
            var publisher = await _publisherService.AddAsync();
            test.PublisherId = publisher.Id;

            await _dbContext.AddAsync(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {space.PublisherId, course.PublisherId, publisher.Id, test.Room!.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        public async Task<ActionResultModel<Test>> Add(Examination examination, TestAddModel model, User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));
       
            var space = examination.Space;
            var test = await CreateTest(space, model);
            test.Examination = examination;
            var publisher = await _publisherService.AddAsync();
            test.PublisherId = publisher.Id;

            await _dbContext.AddAsync(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {space.PublisherId, examination.PublisherId, publisher.Id, test.Room!.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        public async Task<ActionResultModel<Test>> Add(Examination examination, Course course, TestAddModel model, User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.IsTrue(examination.SpaceId == course.SpaceId, "Incompatible entity");
            
            var space = examination.Space;
            var test = await CreateTest(space, model);
            test.Examination = examination;
            test.Course = course;
            var publisher = await _publisherService.AddAsync();
            test.PublisherId = publisher.Id;

            await _dbContext.AddAsync(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {
                space.PublisherId, 
                examination.PublisherId, 
                publisher.Id, 
                course.PublisherId,
                test.Room!.PublisherId
            };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        public async Task<ActionResultModel<Test>> Add(Space space, Course? course, Examination? examination, TestAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));

            var room = await _roomService.GetRoomAsync(model.RoomId);
            if (room.SpaceId != space.Id)
            {
                throw new IncompatibleEntityException(room, space);
            }

            if (course != null && course.SpaceId != space.Id)
            {
                throw new IncompatibleEntityException(course, space);
            }
            
            if (examination != null && examination.SpaceId != space.Id)
            {
                throw new IncompatibleEntityException(examination, space);
            }

            var publisher =  await _publisherService.AddAsync();
            Test test = new()
            {
                Space = space,
                Room = room,
                Course = course,
                Examination = examination,
                Name = model.Name,
                StartAt = model.StartAt,
                Coefficient = model.Coefficient,
                Radical = model.Radical,
                Duration = model.Duration,
                PublisherId = publisher.Id
            };
            await _dbContext.AddAsync(test);

            await _dbContext.SaveChangesAsync();
            _logger.Log(LogLevel.Information, "New test");

            var publisherIds = new List<string> {space.PublisherId, room.PublisherId, publisher.Id};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);

            return new ActionResultModel<Test>(test, @event);
        }

        public async Task<Test> CreateTest(Space space, TestAddModel model)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));
            var room = await _roomService.GetRoomAsync(model.RoomId);
            if (room.SpaceId != space.Id)
            {
                throw new IncompatibleEntityException(room, space);
            }
            
            return new Test()
            {
                Space = space,
                Room = room,
                Name = model.Name,
                StartAt = model.StartAt,
                Coefficient = model.Coefficient,
                Radical = model.Radical,
                Duration = model.Duration
            };
        }
        public async Task<ICollection<Test>> AddCourses(Examination examination)
        {
            var courses = await _dbContext.Set<Course>()
                .Where(c => c.SpaceId == examination.SpaceId)
                .Include(c => c.Space)
                .ToListAsync();

            var tests = new List<Test>();

            foreach (Course course in courses)
            {
                
            }

            throw new NotImplementedException();
        }

        private async Task _SetRoomAsync(Test test, ulong roomId)
        {
            AssertHelper.NotNull(test, nameof(test));
            
            var room = await _dbContext.Set<Room>().FindAsync(roomId);
            if (room == null)
            {
                throw new ElementNotFoundException("RoomNotFoundById", roomId);
            }
            
            if (test.SpaceId != room.SpaceId)
            {
                throw new IncompatibleEntityException(test, room);
            }

            test.Room = room;
        }
        
        
        private async Task _SetCourseAsync(Test test, ulong courseId)
        {
            AssertHelper.NotNull(test, nameof(test));
            
            var course = await _dbContext.Set<Course>().FindAsync(courseId);
            if (course == null)
            {
                throw new ElementNotFoundException("CourseNotFoundById", courseId);
            }
            
            if (test.SpaceId != course.SpaceId)
            {
                throw new IncompatibleEntityException(test, course);
            }

            test.Course = course;
        }

        public async Task<bool> ContainsAsync(Examination examination, string name)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNullOrWhiteSpace(name, nameof(name));

            string normalized = name.Normalize().ToUpper();
            return await _dbContext.Set<Test>()
                .AnyAsync(p => examination.Equals(p.Examination) && p.Name == normalized);
        }

        public async Task DeleteAsync(Test test)
        {
            var testGroups = _dbContext.Set<TestGroup>().Where(g => test.Equals(g.Test));
            
            _dbContext.RemoveRange(testGroups);
            _dbContext.Remove(test);
            await _dbContext.SaveChangesAsync();
        }
    }
}