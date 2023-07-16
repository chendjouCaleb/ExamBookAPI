using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class TestService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PaperService _paperService;
        private readonly RoomService _roomService;
        private readonly TestSpecialityService _testSpecialityService;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<TestService> _logger;

        public TestService(ApplicationDbContext dbContext, 
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
        
        
        public async Task<Test> GetByIdAsync(ulong id)
        {
            var test = await _dbContext.Set<Test>()
                .Where(c => c.Id == id)
                .Include(c => c.Space)
                .FirstOrDefaultAsync();

            if (test == null)
            {
                throw new ElementNotFoundException("TestNotFoundById", id);
            }

            return test;
        }

        public async Task<Test> CreateTestAsync(Space space, TestAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));

            var rooms = await _roomService.GetRoomsAsync(model.RoomIds);
            AssertHelper.IsTrue(rooms.All(r => r.SpaceId == space.Id), "Bad Room space");
            
            var specialities = await _dbContext.Specialities
                .Where(s => model.SpecialityIds.Contains(s.Id))
                .ToListAsync();
            AssertHelper.IsTrue(specialities.All(s => s.SpaceId == space.Id), "Bad speciality space");
            
            var publisher = await _publisherService.CreateAsync();
            var test = new Test
            {
                Space = space,
                SpaceId = space.Id,
                Name = model.Name,
                NormalizedName = StringHelper.Normalize(model.Name),
                StartAt = model.StartAt,
                Coefficient = model.Coefficient,
                Radical = model.Radical,
                Duration = model.Duration,
                Specialized = model.Specialized,
                PublisherId = publisher.Id,
                Publisher = publisher
            };

            var testSpecialities = new List<TestSpeciality>();
            if (model.Specialized)
            {
                
                
                foreach (var specialityId in model.SpecialityIds)
                {
                    var speciality = specialities.Find(s => s.Id == specialityId)!;
                    var testSpeciality = await _testSpecialityService.CreateSpeciality(test, speciality);
                    var testSpecialityPublisher = await _publisherService.AddAsync();
                    testSpeciality.Publisher = testSpecialityPublisher;
                    testSpeciality.PublisherId = testSpecialityPublisher.Id;
                    
                    testSpecialities.Add(testSpeciality);
                }
            }
            
            await _dbContext.AddAsync(test);
            await _dbContext.AddRangeAsync(testSpecialities);
            await _dbContext.SaveChangesAsync();

            var publishers = testSpecialities.Select(t => t.Publisher!)
                .Append(publisher)
                .ToList();

            await _publisherService.SaveAll(publishers);

            var publisherIds = ImmutableList<string>.Empty
                .Add(space.PublisherId)
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                .AddRange(publishers.Select(p => p.Id).ToList());
                
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }


         public async Task<ActionResultModel<Test>> AddAsync(Space space, Course course,
             TestAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));

            var courseSpecialities = await _dbContext.CourseSpecialities
                .Include(cs => cs.Speciality)
                .Where(cs => cs.CourseId == course.Id)
                .ToListAsync();
            var specialities = courseSpecialities.Select(cs => cs.Speciality!).ToList();
         
            var publisher = await _publisherService.CreateAsync();
            var test = new Test
            {
                Space = space,
                SpaceId = space.Id,
                Name = course.Name,
                NormalizedName = StringHelper.Normalize(model.Name),
                StartAt = model.StartAt,
                Coefficient = model.Coefficient,
                Radical = model.Radical,
                Duration = model.Duration,
                Specialized = model.Specialized,
                PublisherId = publisher.Id,
                Publisher = publisher
            };

            var testSpecialities = new List<TestSpeciality>();
            if (model.Specialized)
            {
                foreach (var specialityId in model.SpecialityIds)
                {
                    var speciality = specialities.Find(s => s.Id == specialityId)!;
                    var testSpeciality = await _testSpecialityService.CreateSpeciality(test, speciality);
                    var testSpecialityPublisher = await _publisherService.AddAsync();
                    testSpeciality.Publisher = testSpecialityPublisher;
                    testSpeciality.PublisherId = testSpecialityPublisher.Id;
                    
                    testSpecialities.Add(testSpeciality);
                }
            }
            
            await _dbContext.AddAsync(test);
            await _dbContext.AddRangeAsync(testSpecialities);
            await _dbContext.SaveChangesAsync();

            var publishers = testSpecialities.Select(t => t.Publisher!)
                .Append(publisher)
                .ToList();

            await _publisherService.SaveAll(publishers);

            var publisherIds = ImmutableList<string>.Empty
                .Add(space.PublisherId)
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                .AddRange(publishers.Select(p => p.Id).ToList());
                
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        public async Task SpecializeAsync(Test test, List<Speciality> specialities, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.IsTrue(test.ExaminationId == null);
            AssertHelper.IsTrue(specialities.Count > 0);
            AssertHelper.IsFalse(test.Specialized);
            AssertHelper.IsTrue(specialities.TrueForAll(s => s.SpaceId == test.Id));

            test.Specialized = true;

            var testSpecialities = new List<TestSpeciality>();

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

            var publisherIds = new List<string> {space.PublisherId, course.PublisherId, publisher.Id };
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

            var publisherIds = new List<string> {space.PublisherId, examination.PublisherId, publisher.Id};
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
                course.PublisherId
            };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        public async Task<ActionResultModel<Test>> Add(Space space, Course? course, Examination? examination, TestAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));

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

            var publisherIds = new List<string> {space.PublisherId, publisher.Id};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);

            return new ActionResultModel<Test>(test, @event);
        }

        public async Task<Test> CreateTest(Space space, TestAddModel model)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));
           
           
            return new Test
            {
                Space = space,
                Name = model.Name,
                StartAt = model.StartAt,
                Coefficient = model.Coefficient,
                Radical = model.Radical,
                Duration = model.Duration
            };
        }

        public TestGroup CreateTestGroup(Test test, Room room, uint index)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(room, nameof(room));
            AssertHelper.IsTrue(test.SpaceId == room.Id);

            TestGroup testGroup = new()
            {
                Test = test,
                Room = room,
                Index = index
            };

            return testGroup;
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
        
        
        public async Task<Event> ChangeNameAsync(Test test, string name, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));

            var eventData = new ChangeValueData<string>(test.Name, name);

            test.Name = name;
            test.NormalizedName = StringHelper.Normalize(name);
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                test.PublisherId, 
                test.Space.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_CHANGE_NAME", eventData);
        }
        
        
        public async Task<Event> ChangeCoefficientAsync(Test test, uint coefficient, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            var eventData = new ChangeValueData<uint>(test.Coefficient, coefficient);

            test.Coefficient = coefficient;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                test.PublisherId, 
                test.Space.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_CHANGE_COEFFICIENT", eventData);
        }
        
        public async Task<Event> ChangeRadicalAsync(Test test, uint radical, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));

            var eventData = new ChangeValueData<uint>(test.Radical, radical);

            test.Radical = radical;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                test.PublisherId, 
                test.Space.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_CHANGE_RADICAL", eventData);
        }
        
        
        public async Task<Event> ChangeDurationAsync(Test test, uint duration, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));

            var eventData = new ChangeValueData<uint>(test.Duration, duration);

            test.Duration = duration;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                test.PublisherId, 
                test.Space.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_CHANGE_DURATION", eventData);
        }
        
        
        public async Task<Event> ChangeStartAtAsync(Test test, DateTime startAt, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));

            if (DateTime.UtcNow > startAt)
            {
                throw new IllegalValueException("TestStartDateBeforeNow");
            }

            var eventData = new ChangeValueData<DateTime>(test.StartAt, startAt);

            test.StartAt = startAt;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                test.PublisherId, 
                test.Space.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_CHANGE_START_AT", eventData);
        }
        
        public async Task<Event> LockAsync(Test test, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            
            if (test.IsLock)
            {
                throw new IllegalStateException("TestIsLocked");
            }

            test.IsLock = true;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { test.Space.PublisherId, test.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_LOCK", new {});
        }
        
        
        public async Task<Event> UnLockAsync(Test test, User user)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            
            if (!test.IsLock)
            {
                throw new IllegalStateException("TestIsNotLocked");
            }

            test.IsLock = false;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { test.Space.PublisherId, test.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_UNLOCK", new {});
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