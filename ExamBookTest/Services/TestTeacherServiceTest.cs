using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Asserts;
using Traceability.Models;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
	public class TestTeacherServiceTest
	{
		private IServiceProvider _provider = null!;
		private TestService _testService = null!;
		private CourseService _courseService = null!;
		private TestTeacherService _testTeacherService = null!;
		private CourseTeacherService _courseTeacherService = null!;
		private MemberService _memberService = null!;
		private StudentService _studentService = null!;
		private ExaminationService _examinationService = null!;
		private SpaceService _spaceService = null!;
		private RoomService _roomService = null!;
		private SpecialityService _specialityService = null!;
		private PublisherService _publisherService = null!;
		private EventAssertionsBuilder _eventAssertionsBuilder = null!;

		private ApplicationDbContext _dbContext = null!;
		private User _adminUser = null!;
		private User _user = null!;
		private Member _member = null!;
		private Actor _actor = null!;

		private Space _space = null!;
		private Examination _examination = null!;
		private Speciality _speciality1;
		private Speciality _speciality2;
		private Speciality _speciality3;
		private Room _room = null!;
		private Test _test = null!;
		private Course _course = null!;
		private TestAddModel _model = null!;


		[SetUp]
		public async Task Setup()
		{
			var services = new ServiceCollection();

			_provider = services.Setup();
			_examinationService = _provider.GetRequiredService<ExaminationService>();
			_publisherService = _provider.GetRequiredService<PublisherService>();
			_eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = _provider.GetRequiredService<ApplicationDbContext>();

			var userService = _provider.GetRequiredService<UserService>();
			_spaceService = _provider.GetRequiredService<SpaceService>();
			_memberService = _provider.GetRequiredService<MemberService>();
			_courseService = _provider.GetRequiredService<CourseService>();
			_courseTeacherService = _provider.GetRequiredService<CourseTeacherService>();
			_testService = _provider.GetRequiredService<TestService>();
			_testTeacherService = _provider.GetRequiredService<TestTeacherService>();
			_studentService = _provider.GetRequiredService<StudentService>();
			_roomService = _provider.GetRequiredService<RoomService>();
			_specialityService = _provider.GetRequiredService<SpecialityService>();
			_adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
			_user = await userService.AddUserAsync(ServiceExtensions.UserAddModel1);
			_actor = await userService.GetActor(_adminUser);
			

			var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
			{
				Name = "UY-1, PHILOSOPHY, L1",
				Identifier = "uy1_phi_l1"
			});
			_space = result.Item;
			_member = await _memberService.GetOrAddAsync(_space, _user.Id, _user);

			var roomModel = new RoomAddModel {Capacity = 10, Name = "Room name"};
			_room = (await _roomService.AddRoomAsync(_space, roomModel, _adminUser)).Item;

			_speciality1 = (await _specialityService.AddSpecialityAsync(_space, "Speciality1", _adminUser)).Item;
			_speciality2 = (await _specialityService.AddSpecialityAsync(_space, "Speciality2", _adminUser)).Item;
			_speciality3 = (await _specialityService.AddSpecialityAsync(_space, "Speciality3", _adminUser)).Item;

			var courseResult = await _courseService.AddCourseAsync(_space, new CourseAddModel()
			{
				Code = "125",
				Coefficient = 10,
				Name = "Name",
				SpecialityIds = new HashSet<ulong>() { _speciality1.Id, _speciality2.Id }
			}, _adminUser);

			_course = courseResult.Item;
			_examination = (await _examinationService.AddAsync(_space, new ExaminationAddModel()
			{
				Name = "Exam name",
				SpecialitiyIds = new HashSet<ulong>() {_speciality1.Id, _speciality2.Id}
			}, _adminUser)).Item;
			
			_model = new TestAddModel
			{
				Name = "test name",
				Coefficient = 5,
				Duration = 60,
				StartAt = DateTime.UtcNow.AddMinutes(30),
				Radical = 30
			};

			_test = (await _testService.AddAsync(_examination, _course, _model, new List<ExaminationSpeciality>() { },
				_adminUser)).Item;
		}


		[Test]
		public async Task Get()
		{
			var result = (await _testTeacherService.AddAsync(_test, _member, _adminUser)).Item;
			var courseTeacher = await _testTeacherService.GetAsync(result.Id);
			
			Assert.AreEqual(result.Id, courseTeacher.Id);
		}

		[Test]
		public void GetNotFound_ShouldThrow()
		{
			const ulong notFoundId = ulong.MaxValue;
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _testTeacherService.GetAsync(notFoundId);
			});
			Assert.AreEqual(ex!.Code, "TestTeacherNotFoundById");
			Assert.AreEqual(notFoundId, ex.Params[0]);
		}


		[Test]
		public async Task Contains()
		{
			await _testTeacherService.AddAsync(_test, _member, _adminUser);

			var result = await _testTeacherService.ContainsAsync(_test, _member);
			Assert.True(result);
		}

		[Test]
		public async Task Contains_NotFound_ShouldBeFalse()
		{
			var notFoundMember = await _memberService.GetOrAddAsync(_space, _adminUser.Id, _adminUser);
			var result = await _testTeacherService.ContainsAsync(_test, notFoundMember);
			Assert.False(result);
		}

		[Test]
		public async Task Contains_OnDeletedItem_ShouldBeFalse()
		{
			var courseTeacher = (await _testTeacherService.AddAsync(_test, _member, _adminUser)).Item;
			await _testTeacherService.DeleteAsync(courseTeacher, _adminUser);
			
			var result = await _testTeacherService.ContainsAsync(_test, _member);
			Assert.False(result);
		}
		

		[Test]
		public async Task Add()
		{
			var result = await _testTeacherService.AddAsync(_test, _member, _adminUser);
			var testTeacher = result.Item;
			var action = result.Event;
			await _dbContext.Entry(testTeacher).ReloadAsync();
			
			Assert.AreEqual(_test.Id, testTeacher.TestId);
			Assert.AreEqual(_member.Id, testTeacher.MemberId);

			var assertions = _eventAssertionsBuilder.Build(action);

			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasPublisherIdAsync(testTeacher.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			await assertions.HasPublisherIdAsync(_member.PublisherId);
			await assertions.HasPublisherIdAsync(_examination.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			assertions.HasName("TEST_TEACHER_ADD");
			assertions.HasData(testTeacher);
		}


		[Test]
		public async Task TryAdd_Twice_ShouldThrow()
		{
			await _testTeacherService.AddAsync(_test, _member, _adminUser);
			var ex = Assert.ThrowsAsync<DuplicateValueException>(async () =>
			{
				await _testTeacherService.AddAsync(_test, _member, _adminUser);
			});

			Assert.AreEqual("TestTeacherDuplicate", ex!.Code);
			Assert.AreEqual(_test, ex.Params[0]);
			Assert.AreEqual(_member, ex.Params[1]);
		}

		[Test]
		public async Task DeleteAsync()
		{
			var testTeacher = (await _testTeacherService.AddAsync(_test, _member, _adminUser)).Item;

			var action = await _testTeacherService.DeleteAsync(testTeacher, _adminUser);
			await _dbContext.Entry(testTeacher).ReloadAsync();
			
			Assert.NotNull(testTeacher.DeletedAt);
			Assert.That(testTeacher.DeletedAt, Is.EqualTo(DateTime.UtcNow).Within(50).Milliseconds);

			var assertions = _eventAssertionsBuilder.Build(action);

			assertions.HasName("TEST_TEACHER_DELETE");
			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			await assertions.HasPublisherIdAsync(_examination.PublisherId);
			await assertions.HasPublisherIdAsync(testTeacher.PublisherId);
			await assertions.HasPublisherIdAsync(_member.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			assertions.HasData(new {TestTeacherId = testTeacher.Id});

		}
	}
}