using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
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
	public class CourseTeacherServiceTest
	{
		private IServiceProvider _provider = null!;
		private CourseClassroomService _courseClassroomService = null!;
		private CourseService _courseService = null!;
		private ClassroomService _classroomService = null!;
		private CourseTeacherService _courseTeacherService = null!;
		private SpaceService _spaceService = null!;
		private MemberService _memberService = null!;
		private SpecialityService _specialityService = null!;
		private EventAssertionsBuilder _eventAssertionsBuilder = null!;

		private DbContext _dbContext = null!;
		private User _adminUser = null!;
		private User _user1 = null!;
		private User _user2 = null!;

		private Member _member1 = null!;
		private Member _member2 = null!;
		private Member _adminMember = null!;

		private Space _space = null!;
		private Course _course = null!;
		private Classroom _classroom = null!;
		private CourseClassroom _courseClassroom = null!;
		private Speciality _speciality1 = null!;
		private Speciality _speciality2 = null!;

		private ICollection<Speciality> _specialities = null!;
		private CourseClassroomAddModel _model = null!;


		[SetUp]
		public async Task Setup()
		{
			var services = new ServiceCollection();

			_provider = services.Setup();
			_eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = _provider.GetRequiredService<DbContext>();

			var userService = _provider.GetRequiredService<UserService>();
			_spaceService = _provider.GetRequiredService<SpaceService>();
			_specialityService = _provider.GetRequiredService<SpecialityService>();
			_memberService = _provider.GetRequiredService<MemberService>();
			_courseTeacherService = _provider.GetRequiredService<CourseTeacherService>();
			_courseService = _provider.GetRequiredService<CourseService>();
			_classroomService = _provider.GetRequiredService<ClassroomService>();
			_courseClassroomService = _provider.GetRequiredService<CourseClassroomService>();
			_adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);

			_user1 = await userService.AddUserAsync(ServiceExtensions.UserAddModel1);
			_user2 = await userService.AddUserAsync(ServiceExtensions.UserAddModel2);

			var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
			{
				Name = "UY-1, PHILOSOPHY, L1",
				Identifier = "uy1_phi_l1"
			});
			_space = result.Item;

			var specialityModel1 = new SpecialityAddModel {Name = "speciality name1"};
			_speciality1 = (await _specialityService.AddSpecialityAsync(_space, specialityModel1, _adminUser)).Item;

			var specialityModel2 = new SpecialityAddModel {Name = "speciality name2"};
			_speciality2 = (await _specialityService.AddSpecialityAsync(_space, specialityModel2, _adminUser)).Item;
			_specialities = new List<Speciality> {_speciality1, _speciality2};

			var adminMemberModel = new MemberAddModel {UserId = _adminUser.Id, IsTeacher = true};
			_adminMember = (await _memberService.AddMemberAsync(_space, adminMemberModel, _adminUser)).Item;

			var memberModel1 = new MemberAddModel {UserId = _user1.Id, IsTeacher = true};
			_member1 = (await _memberService.AddMemberAsync(_space, memberModel1, _adminUser)).Item;

			var memberModel2 = new MemberAddModel {UserId = _user2.Id, IsTeacher = true};
			_member2 = (await _memberService.AddMemberAsync(_space, memberModel2, _adminUser)).Item;

			_classroom = (await _classroomService.AddAsync(_space, new ClassroomAddModel
				{
					Name = "Level 1",
					SpecialityIds = _specialities.Select(s => s.Id).ToHashSet()
				},
				_adminUser)).Item;

			_course = (await _courseService.AddCourseAsync(_space, new CourseAddModel()
			{
				Name = "course name",
				Description = "Course description"
			}, _adminMember)).Item;

			_model = new CourseClassroomAddModel
			{
				Code = "652",
				Coefficient = 12,
				Description = "description"
			};
			_courseClassroom = (await _courseClassroomService.AddAsync(_classroom, _course, _model, _adminMember)).Item;
		}


		[Test]
		public async Task AddCourseTeacher()
		{
			var result = await _courseTeacherService.AddAsync(_courseClassroom, _member1, _adminMember);
			var courseTeacher = result.Item;
			await _dbContext.Entry(courseTeacher).ReloadAsync();

			Assert.AreEqual(_courseClassroom.Id, courseTeacher.CourseClassroomId);
			Assert.AreEqual(_member1.Id, courseTeacher.MemberId);

			var assertions = _eventAssertionsBuilder.Build(result.Event);
			assertions.HasName("COURSE_TEACHER_ADD");
			assertions.HasData(new {CourseTeacherId = courseTeacher.Id});
			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasSubjectIdAsync(courseTeacher.SubjectId);
			await assertions.HasPublisherIdAsync(courseTeacher.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			await assertions.HasPublisherIdAsync(_courseClassroom.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			await assertions.HasPublisherIdAsync(_member1.PublisherId);
		}


		[Test]
		public async Task AddCourseTeachers()
		{
			var course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
			var result = await _courseClassroomService.AddCourseTeachersAsync(course, _members, _adminUser);

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
			var courseMember = (await _courseClassroomService.AddAsync(course, _member1, _adminUser)).Item;
			var @event = await _courseClassroomService.DeleteAsync(courseMember, _adminUser);
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
			await _courseClassroomService.AddAsync(course, _member1, _adminUser);

			var exists = await _courseClassroomService.ContainsAsync(course, _member1);
			Assert.True(exists);
		}


		[Test]
		public async Task CourseTeacherExists_WithDeleted_ShouldBeFalse()
		{
			var course = (await _courseService.AddCourseAsync(_space, _courseAddModel, _adminUser)).Item;
			var courseMember = (await _courseClassroomService.AddAsync(course, _member1, _adminUser)).Item;
			await _courseClassroomService.DeleteAsync(courseMember, _adminUser);

			var exists = await _courseClassroomService.ContainsAsync(course, _member1);
			Assert.False(exists);
		}
	}
}