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
        private readonly MemberService _memberService;
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
            MemberService memberService)
        {
            _dbContext = dbContext;
            _paperService = paperService;
            _logger = logger;
            _eventService = eventService;
            _publisherService = publisherService;
            _roomService = roomService;
            _testSpecialityService = testSpecialityService;
            _memberService = memberService;
        }
        
        
        public async Task<Test> GetByIdAsync(ulong id)
        {
            var test = await _dbContext.Set<Test>()
                .Where(c => c.Id == id)
                .Include(c => c.Space)
                .Include(t => t.Examination)
                .Include(t => t.Course)
                .FirstOrDefaultAsync();

            if (test == null)
            {
                throw new ElementNotFoundException("TestNotFoundById", id);
            }

            return test;
        }

        public async Task<Test> CreateTestAsync(Space space, TestAddModel model, 
            ICollection<Speciality> specialities,
            HashSet<Member> members)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(model, nameof(model));

            AssertHelper.IsTrue(specialities.All(s => s.SpaceId == space.Id), "Bad speciality space");
            AssertHelper.IsTrue(members.All(m => m.SpaceId == space.Id), "Bad member space");
    
            var specialized = specialities.Count > 0;
            var publisher = _publisherService.Create();
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
                Specialized = specialized,
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

        public async Task<ActionResultModel<Test>> AddAsync(Space space, TestAddModel model, 
            ICollection<Speciality> specialities,
            HashSet<Member> members,
            User user)
        {

            var test = await CreateTestAsync(space, model, specialities, members);
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
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                .AddRange(publishers.Select(p => p.Id).ToList());
                
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        

        public async Task<ActionResultModel<Test>> AddAsync(Space space, Course course,
            TestAddModel model,
            HashSet<Member> members,
            User user)
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
            

            var test = await CreateTestAsync(space, model, specialities, members);
            test.Course = course;
            test.CourseId = course.Id;

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
                .AddRange(new []{ space.PublisherId, course.PublisherId })
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                .AddRange(courseSpecialities.Select(cs => cs.PublisherId).ToList())
                .AddRange(publishers.Select(p => p.Id).ToList());
                
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        
        public async Task<ActionResultModel<Test>> AddAsync(Examination examination, 
            TestAddModel model, ICollection<ExaminationSpeciality> examinationSpecialities,
            HashSet<Member> members,
            User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(examinationSpecialities, nameof(examinationSpecialities));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.IsTrue(examinationSpecialities.All(es => es.Speciality != null));

            var space = examination.Space;
            var specialities = examinationSpecialities.Select(es => es.Speciality!).ToList();
            var test = await CreateTestAsync(space, model, specialities, members);


            test.Examination = examination;
            foreach (var testSpeciality in test.TestSpecialities)
            {
                var examinationSpeciality =
                    examinationSpecialities.FirstOrDefault(es => es.SpecialityId == testSpeciality.SpecialityId);

                testSpeciality.ExaminationSpeciality = examinationSpeciality;
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
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                .AddRange(examinationSpecialities.Select(es => es.PublisherId))
                .AddRange(publishers.Select(p => p.Id).ToList());
                
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "TEST_ADD", test);
            return new ActionResultModel<Test>(test, @event);
        }
        
        
        public async Task<ActionResultModel<Test>> AddAsync(Examination examination, 
            Course course, TestAddModel model, ICollection<ExaminationSpeciality> examinationSpecialities,
            HashSet<Member> members,
            User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(examinationSpecialities, nameof(examinationSpecialities));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.IsTrue(examinationSpecialities.All(es => es.Speciality != null));

            var space = examination.Space;
            var specialities = examinationSpecialities.Select(es => es.Speciality!).ToList();
            var specialityIds = specialities.Select(s => s.Id).ToList();
            var courseSpecialities = await _dbContext.CourseSpecialities
                .Where(cs => cs.CourseId == course.Id)
                .Where(cs => specialityIds.Contains(cs.SpecialityId))
                .ToListAsync();
            var test = await CreateTestAsync(space, model, specialities, members);


            test.Examination = examination;
            test.Course = course;
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
                .Add(course.PublisherId)
                .AddRange(specialities.Select(r => r.PublisherId).ToList())
                .AddRange(examinationSpecialities.Select(es => es.PublisherId))
                .AddRange(courseSpecialities.Select(es => es.PublisherId))
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
        
        
        private async Task<Event> AttachCourseAsync(Test test, Course course, User adminUser)
        {
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(adminUser, nameof(adminUser));
            AssertHelper.IsTrue(course.SpaceId == test.SpaceId);

            var testSpecialities = await _dbContext.TestSpecialities
                .Where(ts => ts.TestId == test.Id)
                .ToListAsync();

            var courseSpecialities = await _dbContext.CourseSpecialities
                .Include(cs => cs.Speciality)
                .Where(ts => ts.CourseId == course.Id)
                .ToListAsync();
            
            test.Course = course;
            foreach (var testSpeciality in testSpecialities)
            {
                var courseSpeciality = courseSpecialities
                    .FirstOrDefault(cs => cs.SpecialityId == testSpeciality.SpecialityId);

                if (courseSpeciality != null)
                {
                    testSpeciality.CourseSpeciality = courseSpeciality;
                }
            }

            _dbContext.Update(test);
            await _dbContext.SaveChangesAsync();

            var publisherIds = ImmutableList<string>.Empty
                .AddRange(test.GetPublisherIds())
                .AddRange(courseSpecialities.Select(cs => cs.Speciality!.PublisherId))
                .AddRange(courseSpecialities.Select(cs => cs.PublisherId))
                .AddRange(testSpecialities.Select(cs => cs.PublisherId));

            var eventName = "TEST_ATTACH_COURSE";
            var data = new {CourseId = course.Id, TestId = test.Id};
            return await _eventService.EmitAsync(publisherIds, adminUser.ActorId, eventName, data);

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