using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
    public class TestService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PaperService _paperService;
        private readonly RoomService _roomService;
      
        private readonly TestTeacherService _testTeacherService;
        private readonly TestSpecialityService _testSpecialityService;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<TestService> _logger;

        public TestService(ApplicationDbContext dbContext, 
            PaperService paperService, 
            ILogger<TestService> logger, 
            EventService eventService, 
            PublisherService publisherService, 
            RoomService roomService, 
            TestSpecialityService testSpecialityService, 
            TestTeacherService testTeacherService)
        {
            _dbContext = dbContext;
            _paperService = paperService;
            _logger = logger;
            _eventService = eventService;
            _publisherService = publisherService;
            _roomService = roomService;
            _testSpecialityService = testSpecialityService;
            _testTeacherService = testTeacherService;
        }
        
        
        public async Task<Test> GetByIdAsync(ulong id)
        {
            var test = await _dbContext.Set<Test>()
                .Where(c => c.Id == id)
                .Include(c => c.Space)
                .Include(t => t.Examination)
                .Include(t => t.CourseClassroom)
                .Include(t => t.TestSpecialities)
                
                .FirstOrDefaultAsync();

            if (test == null)
            {
                throw new ElementNotFoundException("TestNotFoundById", id);
            }

            return test;
        }

        
        public async Task<ActionResultModel<Test>> AddAsync(Space space, CourseClassroom courseClassroom,
            TestAddModel model,
            HashSet<Member> members,
            Room room,
            User user)
        {
            AssertHelper.NotNull(room, nameof(room));
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));

            var courseSpecialities = await _dbContext.CourseSpecialities
                .Include(cs => cs.Speciality)
                .Where(cs => cs.CourseClassroomId == courseClassroom.Id)
                .ToListAsync();
            var specialities = courseSpecialities.Select(cs => cs.Speciality!).ToList();
            

            var test = await CreateTestAsync(space, model, specialities, members, room);
            test.CourseClassroom = courseClassroom;
            test.CourseClassroomId = courseClassroom.Id;

            foreach (var testSpeciality in test.TestSpecialities)
            {
                var courseSpeciality = courseSpecialities
                    .Find(cs => cs.SpecialityId == testSpeciality.SpecialityId);
                testSpeciality.CourseSpeciality = courseSpeciality;
            }
            
            await _dbContext.AddAsync(test);
            await _dbContext.AddRangeAsync(test.TestSpecialities);
            await _dbContext.SaveChangesAsync();

            var publishers = ImmutableList.Create<Publisher>()
                .AddRange(test.TestSpecialities.Select(ts => ts.Publisher!))
                .AddRange(test.TestTeachers.Select(tt => tt.Publisher!))
                .Add(test.Publisher!);

            await _publisherService.SaveAllAsync(publishers);

            var publisherIds = ImmutableList<string>.Empty
                .AddRange(new []{ space.PublisherId, courseClassroom.PublisherId })
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                .AddRange(courseSpecialities.Select(cs => cs.PublisherId).ToList())
                .AddRange(publishers.Select(p => p.Id).ToList());
                
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        
        
        public async Task<ActionResultModel<Test>> AddAsync(Examination examination, 
            CourseClassroom courseClassroom, TestAddModel model, 
            ICollection<ExaminationSpeciality> examinationSpecialities,
            HashSet<Member> members,
            Room room,
            User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(examinationSpecialities, nameof(examinationSpecialities));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.IsTrue(examinationSpecialities.All(es => es.Speciality != null));

            var space = examination.Space;
            var specialities = examinationSpecialities.Select(es => es.Speciality!).ToList();
            var specialityIds = specialities.Select(s => s.Id).ToList();
            var courseSpecialities = await _dbContext.CourseSpecialities
                .Where(cs => cs.CourseClassroomId == courseClassroom.Id)
                .Where(cs => specialityIds.Contains(cs.SpecialityId))
                .ToListAsync();
            var test = await CreateTestAsync(space, model, specialities, members, room);


            test.Examination = examination;
            test.CourseClassroom = courseClassroom;
            foreach (var testSpeciality in test.TestSpecialities)
            {
                var examinationSpeciality =
                    examinationSpecialities.FirstOrDefault(es => es.SpecialityId == testSpeciality.SpecialityId);

                var courseSpeciality = courseSpecialities.Find(cs => cs.SpecialityId == testSpeciality.Id);
                
                testSpeciality.ExaminationSpeciality = examinationSpeciality;
                testSpeciality.CourseSpeciality = courseSpeciality;
            }
            
            
            
            await _dbContext.AddAsync(test);
            await _dbContext.AddRangeAsync(test.TestSpecialities);
            await _dbContext.SaveChangesAsync();

            var publishers = ImmutableList.Create<Publisher>()
                .AddRange(test.TestSpecialities.Select(ts => ts.Publisher!))
                .AddRange(test.TestTeachers.Select(tt => tt.Publisher!))
                .Add(test.Publisher!);

            await _publisherService.SaveAllAsync(publishers);

            var publisherIds = ImmutableList<string>.Empty
                .Add(space.PublisherId)
                .Add(examination.PublisherId)
                .Add(courseClassroom.PublisherId)
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                //.AddRange(examinationSpecialities.Select(es => es.PublisherId))
                //.AddRange(courseSpecialities.Select(es => es.PublisherId))
                .AddRange(publishers.Select(p => p.Id).ToList());
                
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        
        public async Task<Test> CreateTestAsync(Space space, TestAddModel model, 
            ICollection<Speciality> specialities,
            HashSet<Member> members, Room room)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(room, nameof(room));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(model, nameof(model));

            AssertHelper.IsTrue(room.SpaceId == space.Id, "Bad room space");
            AssertHelper.IsTrue(specialities.All(s => s.SpaceId == space.Id), "Bad speciality space");
            AssertHelper.IsTrue(members.All(m => m.SpaceId == space.Id), "Bad member space");
    
            var specialized = specialities.Count > 0;
            var publisher = _publisherService.Create("TEST_PUBLISHER");
            var test = new Test
            {
                Space = space,
                SpaceId = space.Id,
                Room = room,
                StartAt = model.StartAt,
                Coefficient = model.Coefficient,
                Radical = model.Radical,
                Duration = model.Duration,
                IsSpecialized = specialized,
                PublisherId = publisher.Id,
                Publisher = publisher
            };

            var testTeachers = new List<TestTeacher>();

            foreach (var member in members)
            {
                testTeachers.Add(await _testTeacherService.CreateAsync(test, member));
            }

            var testSpecialities = new List<TestSpeciality>();
            if (specialized)
            {
                foreach (var speciality in specialities)
                {
                    testSpecialities.Add(await _testSpecialityService.CreateSpeciality(test, speciality));
                }
            }

            test.TestSpecialities = testSpecialities;
            test.TestTeachers = testTeachers;

            return test;
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
        
        
       
        public async Task<Event> ChangeRoomAsync(Test test, Room room, User adminUser)
        {
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            AssertHelper.NotNull(room, nameof(room));
            AssertHelper.NotNull(adminUser, nameof(adminUser));
            AssertHelper.IsTrue(room.SpaceId == test.SpaceId);
            AssertHelper.IsTrue(test.RoomId != room.Id, "Same room");
            var currentRoom = test.Room;
            var eventData = new ChangeRoomData(test.RoomId, room.Id);

            test.Room = room;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = ImmutableList<string>.Empty
                .AddRange(new[] {test.PublisherId, room.PublisherId, test.Space.PublisherId});

            if (test.ExaminationId != null)
            {
                publisherIds = publisherIds.Add(test.Examination!.PublisherId);
            }

            if (currentRoom != null)
            {
                publisherIds = publisherIds.Add(currentRoom.PublisherId);
            }

            const string eventName = "TEST_CHANGE_ROOM";
            return await _eventService.EmitAsync(publisherIds, adminUser.ActorId, eventName, eventData);
        }
        
        
        public async Task<Event> RemoveRoomAsync(Test test, User adminUser)
        {
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            AssertHelper.NotNull(adminUser, nameof(adminUser));
            AssertHelper.IsTrue(test.RoomId != null);
            
            var currentRoom = test.Room!;
            var eventData = new ChangeRoomData(test.RoomId, null);

            test.Room = null;
            test.RoomId = null;
            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = ImmutableList<string>.Empty
                .AddRange(new[] {test.PublisherId, currentRoom.PublisherId, test.Space.PublisherId});

            if (test.ExaminationId != null)
            {
                publisherIds = publisherIds.Add(test.Examination!.PublisherId);
            }

            const string eventName = "TEST_REMOVE_ROOM";
            return await _eventService.EmitAsync(publisherIds, adminUser.ActorId, eventName, eventData);
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