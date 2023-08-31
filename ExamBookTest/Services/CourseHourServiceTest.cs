using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Helpers;
using Traceability.Asserts;
using Traceability.Models;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class CourseHourServiceTest
    {
        private IServiceProvider _provider = null!;
        private CourseService _courseService = null!;
        private SpaceService _spaceService = null!;
        private MemberService _memberService = null!;
        private RoomService _roomService = null!;
        private CourseTeacherService _courseTeacherService = null!;
        private CourseHourService _service = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;

        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private User _user1 = null!;
        private User _user2 = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private Course _course = null!;
        private CourseTeacher _courseTeacher = null!;
        private CourseTeacher _courseTeacher2 = null!;
        private Member _member1 = null!;
        private Member _member2 = null!;
        private Room _room1 = null!;
        private Room _room2 = null!;
        private ICollection<Member> _members = null!;
        private CourseAddModel _courseAddModel = null!;
        private CourseHourAddModel _model = null!;


        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();

            _provider = services.Setup();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _service = _provider.GetRequiredService<CourseHourService>();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _memberService = _provider.GetRequiredService<MemberService>();
            _courseService = _provider.GetRequiredService<CourseService>();
            _courseTeacherService = _provider.GetRequiredService<CourseTeacherService>();
            _roomService = _provider.GetRequiredService<RoomService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _user1 = await userService.AddUserAsync(ServiceExtensions.UserAddModel1);
            _user2 = await userService.AddUserAsync(ServiceExtensions.UserAddModel2);
            _actor = await userService.GetActor(_adminUser);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
            {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            var memberModel1 = new MemberAddModel {UserId = _user1.Id};
            _member1 = (await _memberService.AddMemberAsync(_space, memberModel1, _adminUser)).Item;

            var memberModel2 = new MemberAddModel {UserId = _user2.Id};
            _member2 = (await _memberService.AddMemberAsync(_space, memberModel2, _adminUser)).Item;
            _members = new List<Member> {_member1, _member2};

            _room1 = (await _roomService.AddAsync(_space, new RoomAddModel("Room 1", 10), _adminUser)).Item;
            _room2 = (await _roomService.AddAsync(_space, new RoomAddModel("Room 2", 10), _adminUser)).Item;

            _courseAddModel = new CourseAddModel
            {
                Name = "first name",
                Code = "652",
                Coefficient = 12,
                Description = "description"
            };

            _course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
            _courseTeacher = (await _courseTeacherService.AddAsync(_course, _member1, _adminUser)).Item;
            _courseTeacher2 = (await _courseTeacherService.AddAsync(_course, _member2, _adminUser)).Item;

            _model = new CourseHourAddModel
            {
                DayOfWeek = DayOfWeek.Friday,
                StartHour = new TimeOnly(8,00),
                EndHour = new TimeOnly(10, 00),
                RoomId = _room1.Id,
                CourseTeacherId = _courseTeacher.Id
            };

        }

        

        [Test]
        public async Task AddCourseHour()
        {
            var result = await _service.AddAsync(_course, _model, _adminUser);
            var courseHour = result.Item;
            await _dbContext.Entry(courseHour).ReloadAsync();

            Assert.AreEqual(_space.Id, courseHour.SpaceId);
            Assert.AreEqual(_course.Id, courseHour.CourseId);
            Assert.AreEqual(_model.RoomId, courseHour.RoomId);
            Assert.AreEqual(_model.CourseTeacherId, courseHour.CourseTeacherId);
            
            Assert.AreEqual(_model.EndHour, courseHour.EndHour);
            Assert.AreEqual(_model.StartHour, courseHour.StartHour);
            Assert.AreEqual(_model.DayOfWeek, courseHour.DayOfWeek);

            var publisher = await _publisherService.GetByIdAsync(courseHour.PublisherId);
            var coursePublisher = await _publisherService.GetByIdAsync(_course.PublisherId);
            var memberPublisher = await _publisherService.GetByIdAsync(_member1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("COURSE_HOUR_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(memberPublisher)
                .HasPublisher(coursePublisher)
                .HasData(courseHour);
        }
        
        
        [Test]
        public async Task ChangeCourseHourRoom()
        {
            var courseHour = (await _service.AddAsync(_course, _model, _adminUser)).Item;

            var eventData = new ChangeValueData<ulong?>(courseHour.RoomId, _room2.Id);
            var changeEvent = await _service.ChangeRoomAsync(courseHour, _room2, _adminUser);
            await _dbContext.Entry(courseHour).ReloadAsync();

            Assert.AreEqual(_room2.Id, courseHour.RoomId);

            var publisher = await _publisherService.GetByIdAsync(courseHour.PublisherId);
            var coursePublisher = await _publisherService.GetByIdAsync(_course.PublisherId);
            var memberPublisher = await _publisherService.GetByIdAsync(_member1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("COURSE_HOUR_CHANGE_ROOM")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(memberPublisher)
                .HasPublisher(coursePublisher)
                .HasData(eventData);
        }

        
        [Test]
        public async Task ChangeCourseHourTeacher()
        {
            var courseHour = (await _service.AddAsync(_course, _model, _adminUser)).Item;
            var eventData = new ChangeValueData<ulong?>(courseHour.CourseTeacherId, _courseTeacher2.Id);
            
            var changeEvent = await _service.ChangeTeacherAsync(courseHour, _courseTeacher2, _adminUser);
            await _dbContext.Entry(courseHour).ReloadAsync();

            Assert.AreEqual(_courseTeacher2.Id, courseHour.CourseTeacher!.Id);

            var publisher = await _publisherService.GetByIdAsync(courseHour.PublisherId);
            var coursePublisher = await _publisherService.GetByIdAsync(_course.PublisherId);
            var member2Publisher = await _publisherService.GetByIdAsync(_member2.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("COURSE_HOUR_CHANGE_TEACHER")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(member2Publisher)
                .HasPublisher(coursePublisher)
                .HasData(eventData);
        }
        
        

        [Test]
        public async Task DeleteCourseHour()
        {
            var courseHour = (await _service.AddAsync(_course, _model, _adminUser)).Item;
            var @event = await _service.DeleteAsync(courseHour, false, _adminUser);
            

            Assert.Null(await _dbContext.Set<CourseHour>().FindAsync(courseHour.Id));

            var publisher = await _publisherService.GetByIdAsync(_course.PublisherId);
            var coursePublisher = await _publisherService.GetByIdAsync(courseHour.PublisherId);
            var memberPublisher = await _publisherService.GetByIdAsync(_member1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(@event)
                .HasName("COURSE_HOUR_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(coursePublisher)
                .HasPublisher(memberPublisher)
                .HasData(courseHour);
        }

        
    }
}