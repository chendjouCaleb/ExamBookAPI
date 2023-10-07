using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Asserts;
using Traceability.Models;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
	public class CourseSpecialityServiceTest
	{
		private IServiceProvider _provider = null!;
		private CourseService _courseService = null!;
		private CourseSpecialityService _service = null!;
		private SpaceService _spaceService = null!;
		private MemberService _memberService = null!;
		private SpecialityService _specialityService = null!;
		private ClassroomService _classroomService = null!;
		private PublisherService _publisherService = null!;
		private EventAssertionsBuilder _eventAssertionsBuilder = null!;

		private DbContext _dbContext = null!;
		private User _adminUser = null!;
		private Actor _actor = null!;

		private User _user1 = null!;
		private User _user2 = null!;

		private Member _member1 = null!;
		private Member _member2 = null!;
		private Member _adminMember = null!;

		private Space _space = null!;
		private Speciality _speciality1 = null!;
		private Speciality _speciality2 = null!;
		private Classroom _classroom = null!;
		private Course _course = null!;
		private CourseClassroom _courseClassroom = null!;
		private ClassroomSpeciality _classroomSpeciality1 = null!;
		private ClassroomSpeciality _classroomSpeciality2 = null;
		private List<ClassroomSpeciality> _classroomSpecialities = null!;


		private ICollection<Speciality> _specialities = null!;
		private CourseAddModel _model = null!;


		[SetUp]
		public async Task Setup()
		{
			var services = new ServiceCollection();

			_provider = services.Setup();
			_publisherService = _provider.GetRequiredService<PublisherService>();
			_eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = _provider.GetRequiredService<DbContext>();

			var userService = _provider.GetRequiredService<UserService>();
			var roomService = _provider.GetRequiredService<RoomService>();
			var courseClassroomService = _provider.GetRequiredService<CourseClassroomService>();
			_spaceService = _provider.GetRequiredService<SpaceService>();
			_specialityService = _provider.GetRequiredService<SpecialityService>();
			_classroomService = _provider.GetRequiredService<ClassroomService>();
			_memberService = _provider.GetRequiredService<MemberService>();
			_courseService = _provider.GetRequiredService<CourseService>();
			_service = _provider.GetRequiredService<CourseSpecialityService>();
			_adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
			_actor = await userService.GetActor(_adminUser);

			_user1 = await userService.AddUserAsync(ServiceExtensions.UserAddModel1);
			_user2 = await userService.AddUserAsync(ServiceExtensions.UserAddModel2);


			var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
			{
				Name = "UY-1, PHILOSOPHY, L1",
				Identifier = "uy1_phi_l1"
			});
			_space = result.Item;

			var room = (await roomService.AddAsync(_space, new RoomAddModel
			{
				Name = "Room name",
				Capacity = 10
			}, _adminUser)).Item;

			var specialityModel1 = new SpecialityAddModel {Name = "speciality name1"};
			_speciality1 = (await _specialityService.AddSpecialityAsync(_space, specialityModel1, _adminUser)).Item;

			var specialityModel2 = new SpecialityAddModel {Name = "speciality name2"};
			_speciality2 = (await _specialityService.AddSpecialityAsync(_space, specialityModel2, _adminUser)).Item;
			_specialities = new List<Speciality> {_speciality1, _speciality2};

			var memberModel1 = new MemberAddModel {UserId = _user1.Id, IsTeacher = true};
			_member1 = (await _memberService.AddMemberAsync(_space, memberModel1, _adminUser)).Item;

			var memberModel2 = new MemberAddModel {UserId = _user2.Id, IsTeacher = true};
			_member2 = (await _memberService.AddMemberAsync(_space, memberModel2, _adminUser)).Item;

			_classroom = (await _classroomService.AddAsync(_space, new ClassroomAddModel
			{
				Name = "Level 1",
				RoomId = room.Id,
				SpecialityIds = new HashSet<ulong>() {_speciality1.Id, _speciality2.Id}
			}, _adminUser)).Item;
			_classroomSpeciality1 = _classroom.ClassroomSpecialities[0];
			_classroomSpeciality2 = _classroom.ClassroomSpecialities[2];
			_classroomSpecialities = _classroom.ClassroomSpecialities.ToList();

			_course = (await _courseService.AddCourseAsync(_space, new CourseAddModel
			{
				Name = "first name",
				Description = "description"
			}, _adminMember)).Item;
			_courseClassroom = (await courseClassroomService.AddAsync(_classroom, _course, new CourseClassroomAddModel()
			{
				Code = "14",
				Coefficient = 14,
				Description = "RAS",
				MemberIds = new HashSet<ulong>(),
				SpecialityIds = new HashSet<ulong>()
			}, _adminMember)).Item;
		}

		[Test]
		public async Task GetById()
		{
			var courseSpecialities =
				(await _service.AddAsync(_courseClassroom, _classroomSpecialities, _adminMember)).Item;
			var courseSpeciality = courseSpecialities[0];
			var getResult = await _service.GetAsync(courseSpeciality.Id);

			Assert.AreEqual(courseSpeciality.Id, getResult.Id);
		}

		[Test]
		public void GetById_NotFound_ShouldThrow()
		{
			var notFoundId = ulong.MaxValue;
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () => { await _service.GetAsync(notFoundId); });

			Assert.AreEqual("CourseSpecialityNotFoundById", ex!.Code);
			Assert.AreEqual(notFoundId, ex.Params[0]);
		}


		[Test]
		public async Task AddAsync()
		{
			var result = await _service.AddAsync(_courseClassroom, _classroomSpecialities, _adminMember);
			var courseSpecialities = result.Item;

			var assertions = _eventAssertionsBuilder.Build(result.Event);
			assertions.HasName("COURSE_SPECIALITY_ADD");
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasPublisherIdAsync(_adminUser.ActorId);

			foreach (var courseSpeciality in courseSpecialities)
			{
				await _dbContext.Entry(courseSpeciality).ReloadAsync();
				var classroomSpeciality =
					_classroomSpecialities.Find(cs => cs.Id == courseSpeciality.ClassroomSpecialityId);

				Assert.NotNull(classroomSpeciality);
				Assert.AreEqual(_courseClassroom.ClassroomId, classroomSpeciality!.ClassroomId);
				Assert.AreEqual(_courseClassroom.Id, courseSpeciality.CourseClassroomId);

				await assertions.HasPublisherIdAsync(courseSpeciality.PublisherId);
				await assertions.HasPublisherIdAsync(courseSpeciality.ClassroomSpeciality.PublisherId);
				await assertions.HasPublisherIdAsync(courseSpeciality.ClassroomSpeciality.Speciality!.PublisherId);
			}

			await assertions.HasPublisherIdAsync(_courseClassroom.Course.PublisherId);
			await assertions.HasPublisherIdAsync(_courseClassroom.Classroom.PublisherId);
		}


		[Test]
		public async Task DeleteCourseSpeciality()
		{
			var result = await _service.AddAsync(_courseClassroom, _classroomSpecialities, _adminMember);
			var courseSpeciality = result.Item[0];

			var @event = await _service.DeleteAsync(courseSpeciality, _adminMember);
			await _dbContext.Entry(courseSpeciality).ReloadAsync();

			Assert.NotNull(courseSpeciality.DeletedAt);

			var assertions = _eventAssertionsBuilder.Build(@event);
			assertions.HasName("COURSE_SPECIALITY_DELETE");
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasActorIdAsync(_adminUser.ActorId);

			await assertions.HasPublisherIdAsync(courseSpeciality.PublisherId);
			await assertions.HasPublisherIdAsync(courseSpeciality.ClassroomSpeciality.PublisherId);
			await assertions.HasPublisherIdAsync(courseSpeciality.ClassroomSpeciality.Speciality!.PublisherId);
			await assertions.HasPublisherIdAsync(_courseClassroom.PublisherId);
			await assertions.HasPublisherIdAsync(_courseClassroom.Course.PublisherId);
			await assertions.HasPublisherIdAsync(_courseClassroom.Classroom.PublisherId);
			await assertions.HasPublisherIdAsync(_courseClassroom.Classroom.Space.PublisherId);
		}


		[Test]
		public async Task CourseSpecialityExists()
		{
			await _service.AddAsync(_courseClassroom, _classroomSpecialities, _adminMember);
			var exists = await _service.Exists(_courseClassroom, _classroomSpeciality1);
			Assert.True(exists);
		}
	}
}