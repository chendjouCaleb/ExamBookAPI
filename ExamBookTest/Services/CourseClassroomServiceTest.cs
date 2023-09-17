using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Helpers;
using Traceability.Asserts;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
	public class CourseClassroomServiceTest
	{
		private IServiceProvider _provider = null!;
		private CourseClassroomService _service = null!;
		private CourseService _courseService = null!;
		private ClassroomService _classroomService = null!;
		private CourseTeacherService _courseTeacherService = null!;
		private SpaceService _spaceService = null!;
		private MemberService _memberService = null!;
		private SpecialityService _specialityService = null!;
		private PublisherService _publisherService = null!;
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
		private Speciality _speciality1 = null!;
		private Speciality _speciality2 = null!;

		private ICollection<Speciality> _specialities = null!;
		private CourseClassroomAddModel _model = null!;


		[SetUp]
		public async Task Setup()
		{
			var services = new ServiceCollection();

			_provider = services.Setup();
			_publisherService = _provider.GetRequiredService<PublisherService>();
			_eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = _provider.GetRequiredService<DbContext>();

			var userService = _provider.GetRequiredService<UserService>();
			_spaceService = _provider.GetRequiredService<SpaceService>();
			_specialityService = _provider.GetRequiredService<SpecialityService>();
			_memberService = _provider.GetRequiredService<MemberService>();
			_courseTeacherService = _provider.GetRequiredService<CourseTeacherService>();
			_courseService = _provider.GetRequiredService<CourseService>();
			_service = _provider.GetRequiredService<CourseClassroomService>();
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


			_classroom = (await _classroomService.AddAsync(_space, new ClassroomAddModel()
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

			_model = new CourseClassroomAddModel()
			{
				Code = "652",
				Coefficient = 12,
				Description = "description"
			};
		}

		[Test]
		public async Task GetById()
		{
			var course = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;
			var getResult = await _service.GetAsync(course.Id);

			Assert.AreEqual(course.Id, getResult.Id);
		}

		[Test]
		public void GetById_NotFound_ShouldThrow()
		{
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _service.GetAsync(ulong.MaxValue);
			});

			Assert.AreEqual("CourseNotFoundById", ex!.Code);
			Assert.AreEqual(ulong.MaxValue, ex.Params[0]);
		}


		[Test]
		public async Task AddCourseAsync()
		{
			_model.MemberIds = new HashSet<ulong> {_member1.Id, _member2.Id};
			var result = await _service.AddAsync(_classroom, _course, _model, _adminMember);
			var courseClassroom = result.Item;
			await _dbContext.Entry(courseClassroom).ReloadAsync();

			Assert.AreEqual(_classroom.Id, courseClassroom.ClassroomId);
			Assert.AreEqual(_course.Id, courseClassroom.CourseId);
			Assert.AreEqual(_model.Code, courseClassroom.Code);
			Assert.AreEqual(StringHelper.Normalize(_model.Code), courseClassroom.NormalizedCode);
			Assert.AreEqual(_model.Coefficient, courseClassroom.Coefficient);
			Assert.NotZero(courseClassroom.Coefficient);
			Assert.AreEqual(_model.Description, courseClassroom.Description);
			
			Assert.IsNotEmpty(courseClassroom.PublisherId);
			Assert.IsNotEmpty(courseClassroom.SubjectId);

			Assert.True(await _courseTeacherService.ContainsAsync(courseClassroom, _member1));
			Assert.True(await _courseTeacherService.ContainsAsync(courseClassroom, _member2));

			var assertions = _eventAssertionsBuilder.Build(result.Event)
				.HasName("COURSE_CLASSROOM_ADD");
			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasSubjectIdAsync(courseClassroom.SubjectId);
			await assertions.HasPublisherIdAsync(courseClassroom.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			assertions.HasData(courseClassroom);
		}


		[Test]
		public async Task TryAddCourse_WithUsedCode_ShouldThrow()
		{
			await _service.AddAsync(_classroom, _course, _model, _adminMember);

			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
			{
				await _service.AddAsync(_classroom, _course, _model, _adminMember);
			});
			Assert.AreEqual("CourseClassroomCodeUsed", ex!.Code);
			Assert.AreEqual(_classroom, ex.Params[0]);
			Assert.AreEqual(_model.Code, ex.Params[1]);
		}



		[Test]
		public async Task ChangeCourseCode()
		{
			var courseClassroom = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;
			var newCode = "9632854";

			var eventData = new ChangeValueData<string>(courseClassroom.Code, newCode);
			var changeEvent = await _service.ChangeCodeAsync(courseClassroom, newCode, _adminMember);
			await _dbContext.Entry(courseClassroom).ReloadAsync();

			Assert.AreEqual(newCode, courseClassroom.Code);
			Assert.AreEqual(StringHelper.Normalize(newCode), courseClassroom.NormalizedCode);
			
			var assertions = _eventAssertionsBuilder.Build(changeEvent);
			assertions.HasName("COURSE_CLASSROOM_CHANGE_CODE");
			assertions.HasData(eventData);
			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasSubjectIdAsync(courseClassroom.SubjectId);
			await assertions.HasPublisherIdAsync(courseClassroom.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
		}


		[Test]
		public async Task TryChangeCourseCode_WithUsedCode_ShouldThrow()
		{
			var course = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;

			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
			{
				await _service.ChangeCodeAsync(course, course.Code, _adminMember);
			});
			Assert.AreEqual("CourseClassroomCodeUsed", ex!.Code);
			Assert.AreEqual(_classroom, ex.Params[0]);
			Assert.AreEqual(course.Code, ex.Params[1]);
		}

	


		[Test]
		public async Task ChangeCourseDescription()
		{
			var courseClassroom = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;
			var newDescription = "9632854";

			var eventData = new ChangeValueData<string>(courseClassroom.Description, newDescription);
			var changeEvent = await _service.ChangeDescriptionAsync(courseClassroom, newDescription, _adminMember);
			await _dbContext.Entry(courseClassroom).ReloadAsync();

			Assert.AreEqual(newDescription, courseClassroom.Description);

			var assertions = _eventAssertionsBuilder.Build(changeEvent);
			assertions.HasName("COURSE_CLASSROOM_CHANGE_DESCRIPTION");
			assertions.HasData(eventData);
			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasSubjectIdAsync(courseClassroom.SubjectId);
			await assertions.HasPublisherIdAsync(courseClassroom.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
		}


		[Test]
		public async Task ChangeCourseCoefficient()
		{
			var courseClassroom = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;
			uint newCoefficient = 20;

			var eventData = new ChangeValueData<uint>(courseClassroom.Coefficient, newCoefficient);
			var changeEvent = await _service.ChangeCoefficientAsync(courseClassroom, newCoefficient, _adminMember);

			await _dbContext.Entry(courseClassroom).ReloadAsync();

			Assert.AreEqual(newCoefficient, courseClassroom.Coefficient);

			var assertions = _eventAssertionsBuilder.Build(changeEvent);
			assertions.HasName("COURSE_CLASSROOM_CHANGE_DESCRIPTION");
			assertions.HasData(eventData);
			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasSubjectIdAsync(courseClassroom.SubjectId);
			await assertions.HasPublisherIdAsync(courseClassroom.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
		}

		[Test]
		public async Task DeleteCourseAsync()
		{
			var result = await _service.AddAsync(_classroom, _course, _model, _adminMember);
			var course = result.Item;

			var deleteEvent = await _service.DeleteAsync(course, _adminUser);
			await _dbContext.Entry(course).ReloadAsync();

			Assert.NotNull(course.DeletedAt);
			Assert.AreEqual("", course.Name);
			Assert.AreEqual("", course.NormalizedName);
			Assert.AreEqual("", course.Code);
			Assert.AreEqual("", course.NormalizedCode);
			Assert.AreEqual("", course.Description);
			Assert.AreEqual(0, course.Coefficient);

			var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
			var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

			_eventAssertionsBuilder.Build(deleteEvent)
				.HasName("COURSE_DELETE")
				.HasActor(_actor)
				.HasPublisher(publisher)
				.HasPublisher(spacePublisher)
				.HasData(course);
		}


		[Test]
		public async Task FindCourseByCode()
		{
			var createdCourse = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;

			var course = await _service.GetCourseByCodeAsync(_space, createdCourse.Code);
			Assert.AreEqual(createdCourse.Id, course.Id);
		}

		[Test]
		public void FindCourseByCode_NotFound_ShouldThrow()
		{
			var code = Guid.NewGuid().ToString();
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _service.GetCourseByCodeAsync(_space, code);
			});
			Assert.AreEqual("CourseNotFoundByCode", ex!.Message);
			Assert.AreEqual(code, ex.Params[0]);
		}


		[Test]
		public async Task FindCourseByName()
		{
			var createdCourse = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;

			var course = await _service.GetByNameAsync(_space, createdCourse.Name);
			Assert.AreEqual(createdCourse.Id, course.Id);
		}

		[Test]
		public void FindCourseByName_NotFound_ShouldThrow()
		{
			var name = Guid.NewGuid().ToString();
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _service.GetByNameAsync(_space, name);
			});
			Assert.AreEqual("CourseNotFoundByName", ex!.Message);
			Assert.AreEqual(name, ex.Params[0]);
		}


		[Test]
		public async Task IsCourseByCode()
		{
			var course = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;
			var hasCourse = await _service.ContainsByCode(_space, course.Code);
			Assert.True(hasCourse);
		}


		[Test]
		public async Task IsCourseByCode_WithNonCourse_ShouldBeFalse()
		{
			var hasCourse = await _service.ContainsByCode(_space, "5D");
			Assert.False(hasCourse);
		}


		[Test]
		public async Task IsCourseByName()
		{
			var course = (await _service.AddAsync(_classroom, _course, _model, _adminMember)).Item;
			var hasCourse = await _service.ContainsByName(_space, course.Name);
			Assert.True(hasCourse);
		}


		[Test]
		public async Task IsCourseByName_WithNonCourse_ShouldBeFalse()
		{
			var hasCourse = await _service.ContainsByName(_space, "5D");
			Assert.False(hasCourse);
		}


		[Test]
		public async Task AddCourseSpeciality()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
			var result = await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser);
			var courseSpeciality = result.Item;
			await _dbContext.Entry(courseSpeciality).ReloadAsync();

			Assert.AreEqual(course.Id, courseSpeciality.CourseId);
			Assert.AreEqual(_speciality1.Id, courseSpeciality.SpecialityId);

			var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
			var specialityPublisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
			var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

			_eventAssertionsBuilder.Build(result.Event)
				.HasName("COURSE_SPECIALITY_ADD")
				.HasActor(_actor)
				.HasPublisher(publisher)
				.HasPublisher(spacePublisher)
				.HasPublisher(specialityPublisher)
				.HasData(courseSpeciality);
		}


		[Test]
		public async Task AddCourseSpecialities()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
			var result = await _service.AddCourseSpecialitiesAsync(course, _specialities, _adminUser);

			var courseSpecialities = result.Item;

			foreach (var speciality in _specialities)
			{
				var courseSpeciality = courseSpecialities.First(cs => cs.SpecialityId == speciality.Id);
				Assert.AreEqual(course.Id, courseSpeciality.CourseId);
			}

			var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
			var speciality1Publisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
			var speciality2Publisher = await _publisherService.GetByIdAsync(_speciality2.PublisherId);
			var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

			_eventAssertionsBuilder.Build(result.Event)
				.HasName("COURSE_SPECIALITIES_ADD")
				.HasActor(_actor)
				.HasPublisher(publisher)
				.HasPublisher(spacePublisher)
				.HasPublisher(speciality1Publisher)
				.HasPublisher(speciality2Publisher)
				.HasData(courseSpecialities);
		}


		[Test]
		public async Task DeleteCourseSpeciality()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
			var courseSpeciality = (await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser)).Item;
			var @event = await _service.DeleteCourseSpecialityAsync(courseSpeciality, _adminUser);
			await _dbContext.Entry(courseSpeciality).ReloadAsync();

			Assert.NotNull(courseSpeciality.DeletedAt);

			var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
			var specialityPublisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
			var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

			_eventAssertionsBuilder.Build(@event)
				.HasName("COURSE_SPECIALITY_DELETE")
				.HasActor(_actor)
				.HasPublisher(publisher)
				.HasPublisher(spacePublisher)
				.HasPublisher(specialityPublisher)
				.HasData(courseSpeciality);
		}


		[Test]
		public async Task CourseSpecialityExists()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
			await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser);

			var exists = await _service.CourseSpecialityExists(course, _speciality1);
			Assert.True(exists);
		}


		[Test]
		public async Task CourseSpecialityExists_WithDeleted_ShouldBeFalse()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
			var courseSpeciality = (await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser)).Item;
			await _service.DeleteCourseSpecialityAsync(courseSpeciality, _adminUser);

			var exists = await _service.CourseSpecialityExists(course, _speciality1);
			Assert.False(exists);
		}
	}
}