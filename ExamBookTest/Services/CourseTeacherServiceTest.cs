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
using Vx.Asserts;
using Vx.Models;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class CourseTeacherServiceTest
    {
        private IServiceProvider _provider = null!;
        private CourseService _courseService = null!;
        private SpaceService _spaceService = null!;
        private MemberService _memberService = null!;
        private CourseTeacherService _service = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;

        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private User _user1 = null!;
        private User _user2 = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private Course _course = null!;
        private Member _member = null!;
        private Member _member1 = null!;
        private Member _member2 = null!;
        private ICollection<Member> _members = null!;
        private CourseAddModel _courseAddModel = null!;


        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();

            _provider = services.Setup();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _service = _provider.GetRequiredService<CourseTeacherService>();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _memberService = _provider.GetRequiredService<MemberService>();
            _courseService = _provider.GetRequiredService<CourseService>();
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

            _courseAddModel = new CourseAddModel
            {
                Name = "first name",
                Code = "652",
                Coefficient = 12,
                Description = "description"
            };

        }

        

        [Test]
        public async Task AddCourseTeacher()
        {
            var course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
            var result = await _service.AddAsync(course, _member1, _adminUser);
            var courseMember = result.Item;
            await _dbContext.Entry(courseMember).ReloadAsync();

            Assert.AreEqual(course.Id, courseMember.CourseId);
            Assert.AreEqual(_member1.Id, courseMember.MemberId);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var memberPublisher = await _publisherService.GetByIdAsync(_member1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("COURSE_TEACHER_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(memberPublisher)
                .HasData(courseMember);
        }


        [Test]
        public async Task AddCourseTeachers()
        {
            var course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
            var result = await _service.AddCourseTeachersAsync(course, _members, _adminUser);

            var courseTeachers = result.Item;

            foreach (var member in _members)
            {
                var courseMember = courseTeachers.First(cs => cs.MemberId == member.Id);
                Assert.AreEqual(course.Id, courseMember.CourseId);
            }

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var member1Publisher = await _publisherService.GetByIdAsync(_member1.PublisherId);
            var member2Publisher = await _publisherService.GetByIdAsync(_member2.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("COURSE_TEACHERS_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(member1Publisher)
                .HasPublisher(member2Publisher)
                .HasData(courseTeachers);
        }


        [Test]
        public async Task DeleteCourseTeacher()
        {
            var course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
            var courseMember = (await _service.AddAsync(course, _member1, _adminUser)).Item;
            var @event = await _service.DeleteAsync(courseMember, _adminUser);
            await _dbContext.Entry(courseMember).ReloadAsync();

            Assert.NotNull(courseMember.DeletedAt);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var memberPublisher = await _publisherService.GetByIdAsync(_member1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(@event)
                .HasName("COURSE_TEACHER_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(memberPublisher)
                .HasData(courseMember);
        }


        [Test]
        public async Task CourseTeacherExists()
        {
            var course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
            await _service.AddAsync(course, _member1, _adminUser);

            var exists = await _service.ContainsAsync(course, _member1);
            Assert.True(exists);
        }


        [Test]
        public async Task CourseTeacherExists_WithDeleted_ShouldBeFalse()
        {
            var course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
            var courseMember = (await _service.AddAsync(course, _member1, _adminUser)).Item;
            await _service.DeleteAsync(courseMember, _adminUser);

            var exists = await _service.ContainsAsync(course, _member1);
            Assert.False(exists);
        }
    }
}